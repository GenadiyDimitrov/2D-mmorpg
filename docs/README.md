# Documentation

Index of everything under `docs/`.

## Start here
- [**Formulas.md**](Formulas.md) — 🔑 **every formula in the game on one page**, in the shortest form
  that is still true: damage, defence, crit, fizzle, debuff landing, interrupt, pools, regen, speed,
  MP cost, mob curves, drop rates. Read this instead of hunting through a thousand lines of comments.
  ⚠ **A formula change updates this file in the same commit** — same rule as the skill CSVs.
- [**Backlog.md**](Backlog.md) — 🔴 **every feature and change still owed, in one flat list.** Bugs are
  not in it (they live in `testing/`). Ids are permanent (`BL-nn`), newest ruling wins, built means
  deleted. **This is the "what do we build next" file.**
- [**BacklogArchive.md**](BacklogArchive.md) — the rulings those entries replaced, and designs that
  were built and then reversed. Read when you wonder *why* something looks the way it does.
- [**RoadmapNext.md**](RoadmapNext.md) — the one-screen current state: what's live, what's next, what's
  blocked. ⚠ Narrative and version-shaped; the backlog above is the authoritative list of what is owed.
- [**CHANGELOG.md**](CHANGELOG.md) — what has been built, newest first.
- [**Roadmap.md**](Roadmap.md) — the full detail behind the digest, plus the archive of past playtest
  queues (Now / Next / Later / Eventually / Blocked / Dropped).

## `guides/` — how to build & run
- [UnityClient.md](guides/UnityClient.md) — build and run the mobile client on a phone.
- [Publishing.md](guides/Publishing.md) — `tools/publish.ps1`: the versioned server zip + APK in `builds/`.
- [unity/EditorSetup.md](unity/EditorSetup.md) — one-time Unity editor / Android SDK setup.

## `testing/` — what to verify

**Three files, and that is the whole folder** (consolidated 2026-08-07 from fourteen):

- [Open-Checklist.md](testing/Open-Checklist.md) — 🔴 **the one he edits on the phone.** Unversioned
  and rolling: everything still untested, rewritten against each new build.
- [TestChecklist.Unity.md](testing/TestChecklist.Unity.md) — the live per-section detail. Section
  numbers here are what the open checklist's ids refer to.
- [Playtest-Archive.md](testing/Playtest-Archive.md) — every **closed** playtest, verbatim, newest
  first ([19](testing/Playtest-Archive.md#playtest-19) · [18](testing/Playtest-Archive.md#playtest-18) ·
  [17](testing/Playtest-Archive.md#playtest-17) · [16](testing/Playtest-Archive.md#playtest-16) ·
  [15](testing/Playtest-Archive.md#playtest-15) · [14](testing/Playtest-Archive.md#playtest-14) ·
  [13](testing/Playtest-Archive.md#playtest-13) · [0.28.76](testing/Playtest-Archive.md#playtest-02876)),
  plus the [pre-Unity server checklist](testing/Playtest-Archive.md#legacy-testchecklist) and the
  [skills-not-in-CSVs audit](testing/Playtest-Archive.md#skills-not-in-csvs). Read for rationale;
  don't work from it.

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
- [Disciplines.md](design/Disciplines.md) — **the** 3rd-class file: the 10 discipline kits, race identity, the 40+ CSV format, engine gaps. (Absorbed `Descipline.md` + `DisciplineIdentity.md`, both deleted 2026-08-14.)
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
project-level docs (`Backlog.md`, `BacklogArchive.md`, `CHANGELOG.md`, `Roadmap.md`,
`RoadmapNext.md`) at the `docs/` root.

**Where a new ask goes.** A feature or a change he asks for → a `BL-nn` entry in `Backlog.md`.
A bug or a "does this work" → `testing/Open-Checklist.md`. A ruling that replaces an earlier one →
rewrite the backlog entry, move the old text to `BacklogArchive.md`. Something shipped →
`CHANGELOG.md`, and delete it from the backlog.
