# Artifacts — the web pages, in one place

Pages published to claude.ai. Each has its source in this folder; the source is the truth, the link is the copy.

| Page | Link | Source | What it is |
|---|---|---|---|
| **World Grid** | https://claude.ai/artifact/Aou5ndma8Z89TW6TDTMb4z | `WorldGrid.html` | `BL-346` map editor: cells, biomes, territories, borders, spawners. **Saves what you edit** (a shared database on the page), so it is the only one here that holds data the file does not. |
| **Skill Tree** | https://claude.ai/artifact/XWdiQ5hCA42HF6qn7zPXBo | `SkillTree.html` | Race → class → every skill and the levels it is learned at. **Generated** — `dotnet run --project tools/SkillCsvSeed -- --skill-tree`, never hand-edit; rebuild + republish after a class-table or face change. |
| **Skill Icons** | https://claude.ai/artifact/LXQ1CA8WEyRuK99FLn73ni | `SkillIcons.html` | All skill icons by class file, with a filter. Icons come from `docs/data/skill_icons.csv`. |
| **Town Plans** | https://claude.ai/artifact/6GVfFFjotDCfKUWZ3pFr5N | `TownPlans.html` | The town layout sketch (walls, X/Y towns) the towns were built from. |

## Who can open them

**All four are private: only you, signed in to claude.ai.** Claude cannot change that; you do it from the page's
**Share** menu (top of the page): pick people, or "anyone with the link". The menu says when a setting is not available
for a page.

- The three read-only pages (Skill Tree, Skill Icons, Town Plans) are safe to share with anyone.
- ⚠ **World Grid** saves edits. Before sharing it, decide whether the other person may only look or may also edit the
  board; the page shows "can read the board but not change it" to someone who may only look.
- Without sharing, the HTML file itself still opens in any browser, but World Grid then shows only the empty board
  (its saved cells live on claude.ai, not in the file).

## Adding one

A new page gets a row here in the same commit that publishes it.
