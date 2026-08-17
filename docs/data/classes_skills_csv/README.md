# The class skill CSVs — what each file is, and which of them are yours

**These files are AUTHORITATIVE.** Nothing in the repo retunes them; the code reads them, never the
other way round. Where there is no CSV, nothing is invented (`BL-02`).

## The 20-35 files (yours, authored)

`fighter 01-15` · `mage 01-15` · `tank 20-35` · `warrior 20-35` · `rogue 20-35` · `nuker 20-35` ·
`cleric 20-35` — the base and 2nd classes. `rogue 20-35` covers **both** the dagger and the bow to
level 40 (the archer merge), which is why there is no separate archer file below 40.
⚠ **`cleric 20-35` was `healer 20-35` until 2026-08-17** (his rename, content unchanged). The 40+ files
are still `healer 40-74` / `76-85` — **cleric** is the 2nd class, **healer** is the discipline above it,
so the two names are not a mistake unless he says so.

## The 40+ files (seeded for you, playtest 23)

Your instruction: *"u can add files next to other skills 20-35.Csv the mele rogues one, one for
archers, one for buffers and one for healers ..with what u have after 40 so I start with them later
on.. `Healers 40-74.csv` and 76-85"*.

| file | discipline(s) it registers | rows seeded |
|---|---|---|
| `melee rogue 40-74` / `76-85` | **Nullblade** (human) · **Phantom** (elf) · **Venomweaver** (ork) | Prowl 40, Vanish 60 |
| `archer 40-74` / `76-85` | **Sharpshooter** (human) · **Trapper** (elf) · **Hunter** (ork) | Signal Flare 60 |
| `healer 40-74` / `76-85` | **Lightbringer** | Rite of Preservation 83 |
| `buffer 40-74` / `76-85` | **Warchanter** | the whole buff ladder, + Madness 76 |

🔑 **A "melee rogue" or "archer" file is THREE disciplines**, because the rogue splits by RACE at 40 —
one melee and one ranged branch per race. Every other group is one discipline for all three races.
That is what collapses 30 third classes into **10 CSVs**; see
[docs/design/Disciplines.md](../../design/Disciplines.md) §1 for the other six and for the six
questions in it still waiting on you.

### The format

The 20-35 header **plus a trailing `RACE` column** — `human` / `elf` / `ork`, or blank for all three.
A skill for two races gets two rows. Everything else behaves as in the 20-35 files, `REPLACES`
included. A skill with more than one rung is written as one row per rung, named `… L2`, `… L3`.

### ⚠ Two things about the seeded content

- **It is what the game ALREADY registers above 40, not a proposed kit.** Nothing was invented to fill
  these in. Four of the eight files are nearly empty, and that is the honest picture: outside the
  Warchanter's buff ladder there is almost nothing above level 40 in this game yet.
- **The rows are your starting point, not a decision.** `Vanish` in particular ships with an **SP cost
  of 1** — the record default — because pricing a 40+ skill is 40+ balance and therefore yours. It is
  visible in the file for exactly that reason.

### Regenerating

`tools/SkillCsvSeed` wrote the eight 40+ files, once. **It refuses to overwrite an existing file**, so
running it again is safe and does nothing — the moment you edit one it is yours, and the tool will not
touch it. (There is a force switch. Do not use it.)
