# Documentation

Index of everything under `docs/`.

## Start here
- [**RoadmapNext.md**](RoadmapNext.md) — the one-screen current state: what's live, what's next, what's
  blocked. **Read this first.**
- [**CHANGELOG.md**](CHANGELOG.md) — what has been built, newest first.
- [**Roadmap.md**](Roadmap.md) — the full detail behind the digest, plus the archive of past playtest
  queues (Now / Next / Later / Eventually / Blocked / Dropped).

## `guides/` — how to build & run
- [UnityClient.md](guides/UnityClient.md) — build and run the mobile client on a phone.
- [Publishing.md](guides/Publishing.md) — `tools/publish.ps1`: the versioned server zip + APK in `builds/`.
- [unity/EditorSetup.md](unity/EditorSetup.md) — one-time Unity editor / Android SDK setup.

## `testing/` — what to verify
- [TestChecklist.md](testing/TestChecklist.md) — the running WPF / server playtest checklist.
- [TestChecklist.Unity.md](testing/TestChecklist.Unity.md) — the Unity client checklist (the live one).
- Playtest reports, verbatim: [Playtest-16.md](testing/Playtest-16.md) ·
  [Playtest-15.md](testing/Playtest-15.md) ·
  [Playtest-14.md](testing/Playtest-14.md) · [Playtest-13.md](testing/Playtest-13.md) ·
  [Playtest-0.28.76.md](testing/Playtest-0.28.76.md).

## `design/` — how systems are meant to work
- [CombatResolution.md](design/CombatResolution.md) — the unified hit/evade/fail resolver.
- [DamageModel.md](design/DamageModel.md) — the `{Flat, Mod}` skill-damage model.
- [StatMods.md](design/StatMods.md) — the unified stat-modifier layer.
- [BuffLadders.md](design/BuffLadders.md) — buff families, ranks, potions and scrolls. ⚠ Carries a
  **REVISED-0.42.0** block at the top: a group is ONE buff again, and the original decision is marked
  reversed. Read the revision before the body.
- [EconomyRework.md](design/EconomyRework.md) — the price ladder and the drop side.
- [RarityLadder.md](design/RarityLadder.md) — the six-quality ladder (shipped 0.29.1-0.32.0).
- [Crafting.md](design/Crafting.md) · [GearLadderAndCrafting.md](design/GearLadderAndCrafting.md) — the
  crafting-primary economy and the gear-ladder gaps it fills.
- [Instances.md](design/Instances.md) — instances & dungeons (design, not built).
- [AutoHunt.md](design/AutoHunt.md) — idle / offline auto-hunt.
- [Regions.md](design/Regions.md) — polygonal fields and towns (not built).
- [RogueArcherMerge.md](design/RogueArcherMerge.md) — one class to 40 (shipped 0.29.0).
- [Disciplines.md](design/Disciplines.md) · [Descipline.md](design/Descipline.md) — 3rd-class discipline design.
- [BareHands.md](design/BareHands.md) · [Unarmored.md](design/Unarmored.md) — unarmed / unarmored penalty investigations.

## `balance/` — measured balance
- [BalanceMatrix.md](balance/BalanceMatrix.md) — the balance audit. Its reasoning holds; some numbers
  are stale (measure with `tools/BalanceMatrix`, don't derive by hand).
- [ExpCurve.md](balance/ExpCurve.md) + [ExpCurve.csv](balance/ExpCurve.csv) — the levelling curve.

## `data/` — source tables
- [classes_skills_csv/](data/classes_skills_csv) — per-class skill kits.
- [gear/](data/gear) — tiered gear sets.
- [mobs/](data/mobs) — mob base stats and passives.

---

New file? Put it where it belongs: a how-to in `guides/`, a what-to-check in `testing/`, a
system spec in `design/`, balance work in `balance/`, source tables in `data/`. Keep this index and
project-level docs (`CHANGELOG.md`, `Roadmap.md`, `RoadmapNext.md`) at the `docs/` root.
