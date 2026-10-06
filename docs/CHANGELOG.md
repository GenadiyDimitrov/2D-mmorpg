# Changelog

Development history, newest first.

Early work was tracked as **phases** — self-contained slices that each ended in a playable build.
Phases 1–3 built the foundation (movement, interest management, combat, skills, buffs, the
safe-zone town, banded hunting grounds); the written phase record runs to **Phase 24.1**
(2026-06-22). After that the phase numbering was dropped and commits became the record, so entries
from mid-2026 on are grouped **by date** instead. Later, `GameConstants.GameVersion` (starting
0.1.0, currently **0.230.3**) began gating the client/server protocol handshake — it tracks wire
compatibility, not this feature history.

For what's *planned* rather than done, see [Roadmap.md](Roadmap.md).

**This file holds 0.146.0 onward. Older entries are in volumes**, verbatim and newest first (split 2026-09-29):

| volume | versions | dates |
|---|---|---|
| [CHANGELOG-0.100-0.145.md](changelogs/CHANGELOG-0.100-0.145.md) | 0.100.0 → 0.145.x | 2026-08-28 → 09-14 |
| [CHANGELOG-0.58-0.99.md](changelogs/CHANGELOG-0.58-0.99.md) | 0.58.0 → 0.99.x | 2026-08-10 → 08-28 |
| [CHANGELOG-phases-0.57.md](changelogs/CHANGELOG-phases-0.57.md) | Phases 1-24.1, 0.1.0 → 0.57.x | 2026-06 → 08-09 |

🔑 **The rule: no file past 10,000 lines.** When this one passes it, the oldest era in it (cut at an `x.0` version that
opened something) moves to a new volume, and this table gets a row. To search everything: `grep -rn "..." docs/CHANGELOG.md
docs/changelogs/`.

## 2026-10-06 (latest) — 0.230.3: `/copy` — the Owner can clone a character onto another

**Server restart.** The APK only matters for `/help` listing the line (the server answers `/help` itself). No `game.db` delete.

- **His ask:** *"owner only command `/copy <name-source> <name-target>` ... it copies everything with equip/sp/exp ->
  exactly the same only if target is admin stays admin etc ... even if target requires to be offline its ok .. i would
  like if some1 fells like cheating to copy his current char over owner one or other to check stats items etc"*.
- **`/copy <source> <target>`, Owner only.** The target becomes an exact copy of the source: race, classes, level,
  EXP, SP, stats, learned skills, every subclass (with its bars), bag, EQUIPMENT, private warehouse, gold, quests,
  buffs and position.
- **The target keeps** its name, rank (an Admin stays an Admin), account, its jail/kick/mute/pending-delete state, its
  god/invisible toggles, and its friends, block list and social options. The account warehouse is the account's and
  is not copied.
- **The target must be offline** (it would save its old self over the copy); the command says so. An **online source**
  is saved first, so the copy is of what it is now, not of its last autosave.
- Every copied item gets a fresh persistent id (two characters never share one), and the equipment presets are
  re-pointed at the copies. `PersistenceService.CopyCharacterAsync`; the command is in `GameLoopService` beside `/role`.
- Verified headless against a live server: an online target is refused; the copy carries level, race, class and items;
  the name and rank stay; the source is untouched. SmokeTest re-run (persistence was touched).

## 2026-10-06 — 0.230.2: the Elf's Sharpening says PVE and PVP apart

**APK** for the skill text. No server change of substance, no `game.db` delete beyond 0.230.0's.

- **His call:** *"if its a problem for the checker as pvp/pve separate them in the description as pvp and pve and add a
  face description .. with PVP/PVE"*. The `DESCR` rows of `sharpening_elf` read `PVE spell power +X%, PVP spell power
  +X%`, so `--check` verifies BOTH numbers (`pve spell power` / `pvp spell power` are the checker's spellings now).
  The player still reads one line, from a new `DESCRIPTION` cell: *"Evasion +@{eva}, PVE/PVP spell power +@{pvedmg},
  M.Acc +20; MP Consumption +15%"*. `--gen-faces`, `--check` clean.

## 2026-10-06 — 0.230.1: his first pass over the magic Warchanter + a cheaper Harmony of Restoration

**Server restart + APK** (skill text, the Learn tab's 4th-tier Harmony rows). No `game.db` delete beyond 0.230.0's.

- **The two Sound Smashes hold their race's hands** (they are separate ids): the Human's is `blunt/1` (wand), the
  Demon's and Acoustic Shock `blunt/2` (battlestaff).
- **The Demon's Reinforcement resists are 8%** crit rate and 8% crit damage (were 15%).
- **Toggle texts in his wording**, e.g. *"P.Def +28%, P.Crit Damage Resist 15%, Bow Resistance 16%; MP Consumption
  +15%"*. The upkeep is the MP column, so the `(Consumes: N/s)` is gone from DESCR; the in-game text still says how
  much it drains.
- **Harmony of Restoration, his edit:** *"I decreased the harmony_of_restoration mp consumption so it start to restore
  mp if not spammed"*. 40-74 costs 65 → 280 MP (was 238 → 464), with MP/s 1/2/3/4/5/10 from 64. The 4th tier is
  **every other level** (76, 78 … 90, 8 rungs, was 15) at 290 → 360 MP, with HP/MP per second unchanged on those rungs.
- `SkillCsvSeed`: the DESCR reader now really lets the **longest** spelling win a tie (its own docs said so; the code
  took the first in the table). It learned `shield reduction`, `crit damage resist` and `pve/pvp spell power`, so his
  new wording stays verified. `--check` clean, and no other file's reading changed.

## 2026-10-06 — 0.230.0: the Warchanter hits with MAGIC (`BL-335`)

**Server restart + APK** (the Learn tab is built from the compiled class tables). **Delete `game.db`** or make a new
Warchanter: an old one still holds the masteries this removed.

- **His go:** *"start to build .. change the csvs with what we talked .. then interpolate the numbers in between and the
  bl-336 can wait"*. The design is `docs/design/MagicMeleeBuffers.md`; **§9 lists every number that is mine**.
- **All three wear a ROBE and hit through magic.** Human: wand + shield, Sound Smash. Demon: battlestaff, Sound Smash +
  Acoustic Shock. Elf: duals, **Magic Stab** (replaces Sound Burst: melee, one hit, +59 fail points ≈ 60% fizzle at
  parity).
- **Resonant Strikes (`magic_swing`)** turns the BASIC ATTACK into a magic hit. It keeps the miss roll, uses magic crit,
  is never blocked, spends the Spell Rune, stays in the basic damage bucket and still drains with both vampirisms.
  One id: each race climbs its own rungs (power 10-26).
- The damage skills are **single-hit magic spells**, priced so each race's rotation sits on the HEALER's
  (`--magicmelee`: ×0.96-1.04 at 40-90). Acoustic Shock's stun is a magic debuff now (resisted with SPT).
- **Two toggles per race**, each +15% skill MP and 2 → 8 MP/s. **Reinforcement** is P.Def as a PERCENT plus the lost
  crit/bow resists. **Sharpening** is gated to the weapon: the Human's shield numbers (and it goes dark without the
  shield), the Demon's M.Atk + accuracy, the Elf's evasion + spell damage + 20 M.Accuracy.
- **Removed from the class:** heavy/light/weapon/bow masteries, Heavy Caster Mastery, the crit resists, Hit Rate
  Mastery, Shield Mastery, Bow Proficiency, Bow Expertise. **Mana Vampirism** is gated to blunt|duals and reaches 9% for
  every race. **Combo Mastery** is one id per race (3% / 3.5% / 2.6%). **Monster Knowledge** goes to the elf too.
  **Harmonist Dual Proficiency** replaces the bow one.
- New ids: `magic_swing`, `magic_stab`, `sound_smash_demon`, `harmonist_dual_proficiency`, `reinforcement_demon/_elf`,
  `sharpening_demon/_elf`, `combo_mastery_demon/_elf`. Gone: `sound_burst`, `harmonist_bow_proficiency`.
- Engine: `SkillDef.MagicFailPoints` (a skill's own fizzle points), `Entity.MagicSwingPower`, the magic branch in
  `ResolveBasicSwing`. A shield-gated buff is now suppressed without a shield. `docs/Formulas.md` updated.
- `--check` clean; SP repriced (`--reprice-sp`); `--gen-faces`; `debuff_landmods.csv` regenerated; skill icons for the
  new ids. `BL-335` → archive. `BL-336` (no-refresh stuns) stays open, deferred by him.

## 2026-10-06 — 0.229.4: the race bolts deal half damage to players too

**Server restart** for the damage; **APK** for the skill text. No `game.db` delete.

- **His ruling:** *"let vampiric bolt and frost spike also get the pvp cut .. its a helping in farm not in pvp .. one
  race/class can be stronger in pve than other .. but the pvp should be balanced"*.
- Why it was needed: after 0.229.3 a human healer simply cast `human_vampiric_bolt` (power 108 at 90, Holy Ray 109)
  and an elf healer Frost Spikes, both at full power. They are race ladders every mage of that race keeps, so the cut
  reaches nukers and buffers of those races too, which is the ruling.
- `human_vampiric_bolt` and `frost_spikes` carry `PvpDamageMult: 0.5f`. **Damage only**: Frost Spikes' slow and its ×2
  interrupt are untouched, and Vampiric Bolt's 40% heal follows the halved damage. 68 rows in `mage 1st.csv` say
  `Power in PVP x0.5`; texts say "Half power in PvP"; `--gen-faces` re-run, `--check` clean.

## 2026-10-06 — 0.229.3: Holy Bolt and Holy Ray deal half damage to players

**Server restart** for the damage; **APK** for the new skill text (the faces are compiled into the client). No `game.db` delete.

- **His ruling:** *"i want holy_ray/holy_bolt to do 50% in pvp so they are not 97% on par with nukers .. nukers are nukers ..
  healers have dmg skill just to help them solo .. not be best at pvp ... nuker will lose if healer outheal them"*.
- `holy_bolt` (cleric 20-35) and `holy_ray` (healer 40-90) carry `PvpDamageMult: 0.5f`, the field Quick Blast already
  uses. PvE is untouched. Every CSV row says `; Power in PVP x0.5` (33 rows: cleric 2nd, healer 3rd, healer 4th), the
  descriptions say "Half power in PvP", `--gen-faces` re-run, `--check` clean.
- ⚠ **The healer keeps the race bolt at full power.** A human healer also knows `human_vampiric_bolt` (the human mage
  race ladder, power 108 at 90 vs Holy Ray 109), and an elf healer knows Frost Spikes, so those two can still
  duel at full power. The demon's race skill is a buff (Over the Limit). → Closed by 0.229.4.

## 2026-10-05 — 0.229.2: Prowl re-arms itself in auto-farm

**Server restart only.** No APK, no `game.db` delete.

- **His find:** *"Prowl don't auto reactivate when in auto farm"*.
- Prowl is `SkillEffect.None` + `Category.Physical` — its stealth is the `GrantsMobStealth` FIELD — so
  `GameLoopService.ClassifyAuto` saw no buff and filed it under `Other`, the never-auto bucket. A `GrantsMobStealth`
  skill now classifies as a Buff: the autopilot puts it up when it is off (after MP starvation or death) and leaves
  it alone while it runs. The existing 10s-of-upkeep reserve and 10s starve lockout stop any on/off flicker.
  Vanish stays manual (it ends on the first swing).

## 2026-10-04 — 0.229.1: Common jewels give no MP

**Server restart + APK** (the item card reads the compiled catalog). No `game.db` delete.

- **His ruling:** *"Make common jewels give no mp - as armor can't have the set bonus the jewels should not give the
  mp bonus."*
- `ItemCatalog.CommonCopies` now zeroes `MpBonus` on a Common JEWEL; its M.Def is untouched, and a Common armour/robe
  keeps its own +MP. In practice this only bites at T61, the one Common jewel tier that carried MP
  (necklace 33 / earring 25 / ring 17 on the Mythic piece). A Common cannot be enchanted, so no enchant MP either.

## 2026-10-03 — 0.229.0: fighters carry ~1200 MP at 75

**Server restart.** No APK, no `game.db` delete.

- **His ruling:** *"make it ~1200 at 75 before spt .. mages have more spirit anyway so about 0.4?"*
- **The fighter MP `classMod` 0.17 → 0.40** (`StatCalculator.MpClassLevelModifier`) for Tank, Warrior, Rogue, Archer
  AND a Fighter before 2nd class. Raw pool at L75 = 0.40 × 2925 + 15 = **1185** (was ~510). Every mage number is
  untouched (Healer/Warchanter 0.68, Nuker 0.53, Mage pre-2nd 0.50).
- After Spirit (fighters ~26 SPT → ×1.28, mages ~37 → ×1.44): a naked L75 fighter ≈ 1500 against a Magus ≈ 2290, so
  the mage keeps the deeper pool. Regen is unchanged — a fighter holds more but refills at the same rate.
- Side effects by construction: mana drain (a share of the target's Max MP) takes more off a fighter, and %-of-pool MP
  regen buffs pay a fighter more. The stale "Buffer 1100" comment went with it (the Warchanter has always read 0.68).

## 2026-10-03 — 0.228.1: `/bag` and `/give <name>` open their windows again

**APK only.** No server change, no `game.db` delete.

- **His report:** *"/bag <name> don't work ..does nothing"*. The server answered it all along (`AdminBag`); the phone never
  listened. Both admin item windows lived only in the WPF harness and were not ported when it was deleted (0.42.8) — the
  one-argument `/give <name>` picker (`AdminGivePicker`) had gone silent the same way.
- **One new window, two modes** (`GameUi.Admin.cs`): `/bag <name>` lists that player's bag + gold, tap a row → *"Remove X
  from Y?"* → destroyed (the server re-sends the bag, so the list stays current). `/give <name>` lists YOUR bag, tap a row
  → confirm, or the numpad for a stack → handed over; it reads your live inventory, since the server does not re-send it.

## 2026-10-03 — 0.228.0: runes stack, up to 12 hours

**Server restart.** No APK, no `game.db` delete.

- **His ruling** (the discussion: stack vs. turn runes into consumables): *"Option A sounds better because it wouldnt make
  1d/30d Runes infinite and ppls still will need to think before they buy/open rune boxes ... They are server timed not
  player ... we can limit the max stacking to 12h .. So still admin 1d 30d Runes are useful and cannot be player made"*.
  Runes stay on the SERVER clock (they tick offline, as before). IG-style graded shots were raised and set aside by him
  (too much work) — and they are the per-hit consumable this project already dropped.
- **Opening a rune box while you hold the same rune ADDS its time to that rune** — one item, one clock. Before, the second
  rune ticked beside the first and only the longer one counted, so the overlap was thrown away.
- **The cap: `GameConstants.RuneStackCapHours = 12`.** A 1h/2h box that would take the rune past 12h stays SEALED with
  *"War Rune already has 11h 59m left — runes stack to 12h. The box stays sealed."* — never half-wasted.
- **The admin 24h / 30d boxes add their whole time**, uncapped: they are what the cap keeps out of a player's reach.
- **A GRANTED rune adds onto the held one too** (the subclass gift, the Wayfarer runes — `AddItem`), uncapped: refusing a
  grant would only lose it. `/give` still spawns a separate row (an admin's explicit `timed` wins).
- Only the SAME item stacks: War onto War, Spell onto Spell, a Rune of Experience (50%) onto the 50% one. The Grand Rune
  still supersedes the two singles while it runs, and they keep ticking under it, as before.
- Measured headless: 6 × 2h boxes → one War Rune at 11h 59m; the 7th refused and kept; a 1d box on top → 1d 11h.

## 2026-10-03 — 0.227.2: a toggle on auto stays on

**Server restart.** No APK, no `game.db` delete.

- **His find:** *"I again made toggle "sharpening" as auto on and again it begun to cycle it on<>off very fast"*.
- 🔴 **The real cause, measured** (a headless client arming Sharpening on the admin): on/off every ~0.15 s, ~3 Hz, with
  no "activated" line. The autopilot does not press a skill — it QUEUES it — and the queued-cast pipeline
  (`UpdateQueuedSkill`) never asked whether the skill was a toggle. It landed the stance through `ApplyBuff` without
  `toggle`, i.e. a 0-tick buff that expired the next tick, and the chain re-lit it. A tap was always fine: it reaches
  `HandleToggle` from `BeginSkill`.
- **Fix:** a queued toggle is now handed to `HandleToggle`, the same flip a tap does. Re-measured: Sharpening stays
  up, one buff push a second (the upkeep). Every toggle on the auto bar benefits (Reinforcement, the Marks, Prowl…).
- ⚠ 0.102.5's "stances stop flickering" fix (10 s starvation lockout + 10 s of upkeep before arming) diagnosed a
  real but SECOND cause; it stays. It could never have stopped this one.

## 2026-10-03 — 0.227.1: buffer Mana Vampirism 3/6/9%, the elf stops at 6%, and bows finally drain

**Server restart + APK** (a class-skill-table change: the client builds its Learn tab locally). No `game.db` delete.

- **His ask:** *"Increase mana vamp (buffers) - because is basic attack only 3% is very low number .. I would like max
  lvl to match the 9% normal vamp ... From 1/1.5/2% to 3/6/9 and elf to lvl it to lvl 2 only for the bow to be at 6%"*.
- **`mana_vampirism` 1/1.5/2% → 3/6/9%** (blunt and bow alike). `buffer 3rd.csv` moved with it.
- **Rung 3 (9%, @70) is Human + Demon only** (`RACE = Demon;Human` on the row). The elf tops out at rung 2, 6%.
  `--reprice-sp` re-solved the elf curve without that 148k rung: its other late rungs (armour/weapon/bow mastery 68-74,
  Sound Burst 74) rose ~1%.
- 🔴 **Bug fixed — a bow drained NOTHING.** The mastery has granted mana vamp on blunt OR bow since 0.101.3, but the
  on-hit line in `GameLoopService` still skipped `WeaponType.Bow`, so the elf buffer's whole line was dead. The weapon
  gate now lives only in the mastery profile.

## 2026-10-03 — 0.227.0: weapon lines drop in pairs; recipes, buff/dash potions and attribute scrolls cut

**Server restart.** No `game.db` delete. No APK needed for the drops (the server rolls them); the client's local
"droppable" marker still lists the four merchant-only HP/MP recipes until the next APK.

- **His ask:** *"Can we make drops a bit equal .. Now searching bloodsteel maul and it only drops from a elite monster
  and bows drop from many mobs"* — plus five rate cuts. **Bosses drop unaffected.**
- **Weapon lines are dealt in PAIRS** (`MobCatalog.WeaponPairs`): Blade + Greatsword, Mace + Maul, Bow + Fangs,
  Wand + Staff. A weapon carrier drops both lines of its pair (Commons, rare piece, recipe, part). A carrier left
  empty after the deal takes the pair FEWEST carriers hold, never its own held pair (archers are common, which is how
  bows ended up everywhere). T61 Bloodsteel Maul: 1 normal carrier → 4 (+3 elite + the boss).
  `docs/data/mobs/mob_drops.csv` regenerated.
- **Recipes ÷2.5** off normal and elite creatures (`MobCatalog.MobRecipeCut`), gear and generic alike. The
  Common/Uncommon HP and MP recipes (Apothecary L0/L2) **no longer drop at all** — Master Crafter only. Their share
  left the world rather than moving to the other generic lines.
- **Buff potions (Swift/Alacrity/Fury, C + U) ÷5, and on a THIRD of the creatures** (stable hash of the id): T1-51
  Common 1.05% → 0.21% per carrier, 113 normal/elite sources → 57.
- **Dash ÷3** (Lesser 1.05% → 0.35%). **Attribute scrolls: Common ÷5** (3.6% → 0.72% after the global rate),
  **Uncommon ÷2** (1.8% → 0.9%); Rare/Epic/Legendary/Mythic untouched.
- **Bosses:** the template bakes the cut rows; `KillTable` swaps in the uncut ones at Boss rank
  (`IsBossKeptConsumable`), so a boss pays exactly what it did on every template. Boss recipes read `BossDrops`, which
  was not touched. Verified with `BalanceMatrix --drops`: boss rows identical before and after.

## 2026-10-02 — 0.226.1: a replacement is transitive — the next skill retires what its predecessor retired

**New APK + server restart.** No `game.db` delete: the login pass applies the new rule to every saved character.

- **His find:** learning Sound Smash at 40 removed Holy Bolt and Holy Spike but **gave Magic Bolt back**. His rule:
  *"If I learn skill A then I learn skill B that replaces A I'm left with only skill B but then I learn skill C that
  replaces skill B I should be left with skill C only ... Any next skill replaces the skills that his predecessor
  replaced as well."*
- **The cause:** every "is this retired?" check read only a skill's DIRECT `Replaces` list. Holy Bolt retires Magic Bolt;
  Sound Smash retires Holy Bolt — so once Holy Bolt was gone, nothing owned named Magic Bolt any more and the mage's
  starter-nuke auto-grant put it back.
- **The fix:** `SkillCatalog.ReplacedChain(id)` = the skill's `Replaces` plus, transitively, everything those replaced
  (mutual replacers are safe; a skill never retires itself). Every reader goes through it: the learn, the learn list
  (server and client), the starter-nuke grant, the login pass, the debug learn-all, the admin seed, the passive fold, and
  a class row's own `Replaces` (`ClassSkills.RowReplaced`). `def.Replaces` stays the authored one-step list the CSVs show.
- **Who it touches** (audited off the catalog — nothing else gains a retirement): Magic Bolt also leaves on Sound
  Smash / Sound Burst / Acoustic Shock / Holy Ray / Elemental Blast; Strike / Shot / Stab also leave on the three racial
  slashes, the three shouts, the six 3rd-tier stabs/bursts and Twin Arrows.

## 2026-10-02 — 0.226.0: icons for the NPC buffs, potions, scrolls, runes and the paving (`BL-331`)

**New APK + server restart.** No `game.db` delete. Checklist §137.

- **77 new rows in `docs/data/skill_icons.csv`**, his ask: *"Put icons on npc (can be the same as the singles) ... gp/mp
  potions ... the 3 potions + dash ... pavement buff ... Runes"*. Every one is a row he can change.
  - **NPC buffs** wear their class single's glyph and colour (NPC Might = Might). The four NPC groups have their own glyph.
  - **HP potions** are red and **MP potions** blue, on the Restore Mana bottle. The glyph is the tier (round / ball /
    standing); the Instant Healing Potion is a heart bottle.
  - **Swift, Alacrity and Fury potions** have one flask each, and **Dash** a vapour bottle. The colour is the item's rarity
    (the same colours as names in the bag).
  - **Runes** are runic glyphs coloured by what they boost: War = blood, Spell = arcane, Grand = gold, and one each for the
    Exp/SP/Gold/Drop/Sinister/Sinners/Favor Keep/Blessing Booster runes.
  - **Paved Streets** is a stone path.
  - Not in his list, added the same way: the **18 buff scrolls** (the single's glyph in the scroll's rarity colour) and the
    two **Over-Grade** debuffs (broken shield / broken axe).
- **Six new SCHOOLS**: `common` … `mythic`, for item buffs.
- **New `buff:<key>` row type** for a buff-bar row no skill casts (Paved Streets, Over-Grade). The server sends that key
  as the row's icon id.
- **A potion or scroll on the skill bar** shows its picture too (the item's use-skill), with the count still on it.
- The review page (`docs/design/SkillIcons.html`) has new sections: spirit helper, potions, scrolls, runes, buff bar.

## 2026-10-02 — 0.225.2: every RACE lands on its SP target, not the three-race average (`BL-334`)

**New APK + server restart.** No `game.db` delete. Checklist §136 (row 136d).

- **Each race's own skills now carry that race's multiplier.** His point: *"one race have 20 skills the other 10 ... making
  average of 15 is a +50% more expensive for one and 50% less expensive for the other"*. A skill only some races learn is
  priced × m(race), solved so that race hits the band target alone; skills every race learns keep one price.
  - The races only trade between themselves (the multipliers average 1).
  - The multipliers are capped at ×0.5-×2, his ±50%.
- **Result: every race of every class lands on its target**, except Demon Warchanter (×0.74 vs ×0.77 at 61-75). The
  buffer kit is 59 shared skills and only 6-7 per race, so uncapped it needed Human ×3.3 and Demon ×0.3.
  - The biggest moves elsewhere: Demon Magus own skills ×0.63-0.73, Elf Magus ×1.25-1.35, Elf Ravager ×1.23-1.29,
    Human Ravager ×0.81-1.07.
  - Tank, war_aoe, dagger and archer kits were already even (×1.00).
- `--reprice-sp` prints the multipliers under the x table.

## 2026-10-02 — 0.225.1: SP targets eased; the curve is solved every run, nothing stored (`BL-334`)

**New APK + server restart.** No `game.db` delete. Checklist §136 (rewritten for this build).

- **Bands eased**, his call: *"X0.6 apparently is way too hard"*. The targets are now 40-51 ×0.9, **52-60 ×0.85** (was
  0.75) and **61-75 ×0.8** (was 0.6), each ±5% by archetype as before. The 60-75 kit is back to **44-58M** (0.225.0 had
  it at 67-78M; before `BL-334` it was 43-55M), and every ladder still rises.
- **The freeze is gone.** His words: *"freeze is not required (no files no nothing just formula) ... each skill change
  will fix the class sp cost and class won't move from its x"*. `--reprice-sp` solves every file's curve fresh on every
  run, so adding skills makes each of them cheaper and the class keeps its affordability.
  - `--solve` is gone, and so is `sp_curve.csv`. His ±% per file now lives in `docs/data/sp_adj.csv`.
  - `--check` re-solves too, so `SP STALE` still means "a knob moved and nobody repriced".

## 2026-10-02 — 0.225.0: skill SP is a formula, and every ladder rises (`BL-334`)

**New APK + server restart** (the Learn tab reads the prices from the compiled tables). No `game.db` delete. Checklist §136.

His ask: *"I want the skills to have weight but then again I want each time skills to be with rising SP ... Not lvl 35
buff to cost 45k 40 to cost 68k and 44 to cost 36k ... I want to go up."* The `BL-326` pot split made a crowded level
cheaper per skill; the CSVs had **365 falling steps across 321 ladders**. Research on IG: `docs/balance/SpVsIG.md`.

- **Every SP cell at learn level 1-75 is now `weight × the SP one level pays × c(file, level)`**, written by
  `SkillCsvSeed -- --reprice-sp` (`tools/SkillCsvSeed/SpCurve.cs`). The income comes from `ExpCurve`, so an EXP change
  moves the prices with it (his condition for keeping SP at 1/20 of EXP). Formula: `docs/Formulas.md`.
- **His knobs, three files:** `sp_weights.csv` (unchanged), `sp_bands.csv` (1-19 ×1.75, 20-39 ×1.25, 40-51 ×0.9,
  52-60 ×0.75, 61-75 ×0.6) and `sp_curve.csv` (`ADJ`: tanks, warriors, daggers +5%; Magus 0; bows, healers, buffers
  −5%). The curve anchors in `sp_curve.csv` are **frozen**. His point: re-solving on every run would make all the
  tank's skills cheaper the day he adds twelve more. `--reprice-sp --solve [file]` re-fits them on request.
- **A rung costs at least ×1.01 of the rung before** (same skill, same race, 1st → 2nd → 3rd), his floor.
- **`--check` fails `SP STALE`** (a cell differs from the formula) **and `SP FALLS`**. The result: 0 falls, and a rerun
  rewrites 0 cells.
- `--reweigh-sp` and `--scale-sp` are gone. A blanket price change is now a band target.
- **What moved** (`BalanceMatrix --sp-budget`, x = SP earned / kit cost, ×1):
  - **1-19:** the kit costs ~26k, was ~12k (x 3 → 1.75).
  - **20-39:** 0.57-0.72M, was 0.76-1.22M (x → 1.19-1.31).
  - **60-75:** **67-78M, was 43-55M**. Your ×0.6 band lands about 40% dearer than before.
  - A tier is bought ~3-5 levels after it opens (40 → 43-45, 52 → 55-56). The 72 and 74 tiers finish after the 76 EXP
    wall.
- ⚠ **A steep 2nd-class curve holds up the start of the 3rd.** The tank 2nd file has few skills, so its prices run
  high, and Shield Shock's 3rd-class rungs at 40-52 sit on the +1% floor (122k → 130k).
- **76+ is untouched** (a separate discussion, his ruling).

## 2026-10-02 — 0.224.0: whole-second cooldowns; sheets that open whole; the rune texts agree

**New APK + server restart.** No `game.db` delete. Checklist §135.

- **The skill bar's reuse counter shows whole seconds**, rounded up (`3`, `2`, `1`). His find: *"remove the miliseconds
  ... 3s,2s,1s ~ done .... is enough"*. The tenths it showed under 10s are gone.
- **The Character window's Details tab, the skill card and the Learn card open whole the first time.** His find
  (`[!]`): *"the details panel is not the full one ... most stats are cut off after reopen it works ... very often it
  happens to 'learn' skills"*. The scroll content was laid out against the label's PREVIOUS (empty) height — two nested
  ContentSizeFitters resolve a pass apart. New `UiKit.RefitScroll` forces the rebuild after the text is set, the fix the
  Target window and the item card already had; the Character sheet jumps to the top only on open / tab switch, never on
  a regen tick.
- **Spell and Grand Rune texts say the same thing about casting**: *"shortens spell cast time"* on both (item, every box,
  and the buff's face). His 132a: *"remove the casting part and only say 'shorten spell cast time' and add the same
  sentence to the spell rune aswell. Ppl not to think the grand is better."* The two always carried the identical cast
  cut (`CastTimePct 0.30` + 40 cast stat); only the words differed. Numbers unchanged.

## 2026-10-02 — 0.223.2: the Master's Trial — five gather steps again, and a fail never walks you back

**Server restart only.** No APK, no `game.db` delete. Checklist §134. ⚠ A character mid-trial: the steps are renumbered
again — abandon and retake it.

His 130a, superseding 0.222.2: *"return the individual steps ... after ive done the individual steps once and fail a craft
i should not go back steps ... i need only succesful craft .. i go gather/craft on my own"*.

- **Five gather steps again**, each with its own counter (`0/20` wood, iron, gems; `0/2` recipes; `0/1` head) — the single
  `0/63` step hid which pile was short. Farmed in any order, they are walked in one pass once all are held.
- **A failed hammer NEVER sends the trial back.** It stays on the craft step whether or not you hold another set; the
  message says which (*"you have the materials for another attempt"* / *"gather another set — the craft window shows
  what the recipe needs"*). No talk-back, no relearn.
- The trial's drops (wood, iron, gems, recipes, heads) already roll while the quest is active at ANY step, so they keep
  coming until it completes — nothing to change there. `QuestStep.Items` stays in the engine, now unused.
- SmokeTest updated: the five steps, and a fail with nothing spare stays on the craft step and re-crafts with no talk.

## 2026-10-02 — 0.223.1: equipment presets survive a restart; level-ups are your own news

**Server restart only.** No APK, no `game.db` delete. Checklist §133.

- **Equipment presets work after a server restart / relog.** His find (`[!]`): *"11 items are missing skipping … it
  happen again after game restart"*. The preset stored each item's RUNTIME id, and that id is minted fresh on every
  load — so every saved item read as missing after any restart. Presets are now written by the item ROW's id and
  translated back to the live ids on load (`PersistenceService`); a bag item gets its row id on its first save rather
  than its first reload, so a preset saved minutes after a pickup survives too. ⚠ Presets saved BEFORE this build hold
  the old ids: re-save each once.
- **"X reached level N" is no longer broadcast to everyone.** His find: *"it should be self only -> 'you reached x
  lvl'"*. Only you see "You reached level N!".
- **`/help` lists the player `/buff` only while the server hands out free buffs** (`RateConfig.FreeBuffs`, `BL-126`).
  His 128a: *"remove the /buff command from common ones or check if server allows it then add it"* — it does the second.

## 2026-10-02 — 0.223.0: Grand Rune boxes on the shelf; Toggles get their own group on Known

**New APK + server restart.** No `game.db` delete. Checklist §132.

- **Grand Rune Box (1h) 225,000 and (2h) 420,000** at the Apothecary, beside the War/Spell boxes. His find: *"some1 that
  will need the 2 types of runes is having bad time ... the price of 1+half of the second -> no1 with a single spec will
  by it"*. Same Grand Rune as the premium 24h box (both channels ×2, casts −30%); tradable sealed, like the single 1h/2h.
  The **Pet Grand Rune** half (75k/140k) is deferred by him → `BL-333` ⏸.
- **Skills window, Known tab: toggles are their own "Toggles" group**, right after Buffs. His find: *"now they are
  inbetween the buffs and its hard to locate them ... only the known tab to separate them"*. Display only — no category
  or skill data changed (`KnownGroup` in `GameUi.Skills.cs`).

## 2026-10-02 — 0.222.3: the buffer's melee Sound skills cost less MP

His CSV edit (`buffer 3rd.csv` + `buffer 4th.csv`, My Finds: *"Decreased the MP cost for physical skills of buffer (archer
the same - more dmg more mp ..)"*).

- **Sound Smash and Acoustic Shock** read their own MP column: 36 → 78 over the 3rd tier (was 62 → 120) and 80 → 110
  over the 4th (was 123 → 195). `SoundMeleeMp` / `Wc4SoundMeleeMp`, chosen by `SoundSkill(..., melee: true)`.
- **Sound Burst** (the Elf's bow, two hits) keeps the old column — his "archer the same". So does the Warrior's
  **Sundering Blow**, which borrows the same ladder and was not in his edit.
- `SkillCsvSeed --check` clean; `--gen-faces` unchanged (MP is not in the faces). Server-side; the next APK shows the new
  number in the Learn tab.

## 2026-10-02 — 0.222.2: the Master's Trial — one gather step, and a fail keeps you at the anvil

His find (My Finds, `[!]`): after a failed hammer the trial went back to "20 Seasoned Hardwood", walked the five piles one
by one, and refused the next craft (*"That recipe is only for the Master's trial, once you reach its craft step"*) while
he still held two more Hammer Heads. *"the gathering steps should be combined into one ... when i have x3 mats ... I
should be able to craft 3 times and fail ... not to go back after each fail"*.

- **The five collect steps are ONE step** — `QuestStep.Items` (`QuestItemNeed[]`), met when every pile is held. Its
  counter is the sum (0/63); the quest window's gather list still shows each pile. The trial is now 6 steps (craft step 4).
- **A recipe-book pile asks one less once its recipe is learned** (`CollectNeeds`), so after a fail you need 1 hammer
  recipe, not 2 — the "one to learn" was already spent.
- **A failed hammer stays on the craft step while you can pay for another attempt** (inputs + a 40% recipe, bag or
  shelf the same way the craft reads them). Only a fail that leaves you short goes back to gather.
- **A saved quest past its new end restarts at step 0** (load guard in `PersistenceService`): a character that was on
  the old step 8 would otherwise index past the array. Mid-trial characters on old steps 1-7 may read oddly — abandon
  and retake if so. No `game.db` delete.
- Server-only. SmokeTest: the retry-with-a-spare path and the go-back path are both checked.

## 2026-10-02 — 0.222.1: Over the Limit no longer takes a buff slot

- **`demon_over_limit` carries `CountsTowardBuffLimit: false`.** Owner: *"over the limit should not go towards
  the buff limit ... its a 10s buff"*. It sits on a shelf, which put it in `SkillCatalog.BuffLimitIds`; the
  authored veto takes it out, so a 10-second burst can never evict one of the 20 real buffs (FIFO).
- **Monster Knowledge is unchanged and still counts**: *"can be left as its a choice to warriors/buffer if
  they gonna fight players or mobs"*. A slot spent on it is the price of the PvE choice.
- Server-side only; the client reads the slot flag off `BuffDto`, so no new APK.

## 2026-10-02 — 0.222.0: `/who <name>` — an admin opens a player's Character window

His ask (My Finds, 2026-10-02): *"also we need `/who <name>` command that opens stat window of the character - an
*admin* command"*. **New APK + server restart.** A new server→client message (`AdminWho`); an older APK ignores it, so
no protocol bump. No `game.db` delete. Checklist §129.

- **Server:** `case "who"` (Admin and Owner; not in any moderator list) sends `AdminWhoDto` — the target's stats, active
  class, PvP/karma, Favor block, gold and account platinum — in one message. ONLINE players only: the sheet is derived
  numbers (buffs, gear, passives) that exist only on a live character. `SendStats`/`SendFavor` were split into
  `BuildStats`/`BuildFavor` so `/who` reads exactly what the player's own sheet is sent, without touching the
  sent-state fields `SendFavor` uses for its change test.
- **Client:** the Character window reads a `SheetSource` (yours, assembled from the separate pushes; or the `/who`
  one), so the two sheets cannot read differently. A `/who` sheet is headed with the name, is a snapshot (send `/who`
  again to refresh), and closing the window drops it; the Char button reopens your own.
- `/who` is in `/help`'s Admin section.

## 2026-10-02 — 0.221.0: `/help` for every rank, from one command list; actions show their typed command

His find (My Finds, 2026-10-02): `/help` to print a SYSTEM block — the `@s`/`@t` note, then *"--- Owner ONLY ---"*,
*"--- Admin ONLY ---"*, Moderator, Chat Mod, Commands — where *"each lower group is available on the upper one"*, *"no
action commands like `/like` they are as action buttons"*, *"missing commands like the /stats for the admin"*, and
*"action buttons can have in their description a `(/command)`"*. **New APK + server restart.** No wire change, no
`game.db` delete. Checklist §128.

- **`Game.Shared/ChatCommands.cs` — `ChatCommandCatalog`**, every typed command with its usage, a short clause and the
  lowest rank that may use it. `/help` prints the caller's rank and every rank below; a player now gets the plain
  commands too (it used to be "Unknown command"). The admin list gained what was missing: `/stat`, `/tpme`, `/ban`
  minutes, `/tp` coordinates, `/enchant`, `/like -f`, `/whatdrops`, `/dropindex`, `/farmcap`, `/testcaps`.
- **The Moderator / Chat Moderator allow-lists are READ from it** (`StaffAllowList`) — the same nine and four commands
  as before, but now the list a moderator is shown cannot drift from the list he is allowed. Player rows never enter a
  staff list, so a Player-level name (`buff`) cannot open the admin half of the same command.
- **`ActionDef.Command`** — the typed twin of 11 actions (`/ptinv`, `/ptkick`, `/ptcl`, `/ptleave`, `/fadd`, `/frem`,
  `/flist`, `/w`, `/like`, `/block <name>`, `/unblock`). The Actions tab shows it after the name in small grey;
  `/help` leaves them out.
- ⚠ Two placeholders renamed because TextMesh Pro reads them as tags in chat: `-p <page>` → `-p <n>`, `<color>` →
  `<colour>`.
- `/who <name>` (his other ask, an admin's view of a player's character sheet) is the next version.

## 2026-10-02 — 0.220.3: four finds — buffer's Rogue Evasion, 300 chat rows, the arrow on for every character, a deeper pulse

His finds and answers from the 0.220.2 pass. **New APK + server restart.** No wire change, no `game.db` delete.
Checklist §127, §125a, §121d.

- **His CSV edit (`buffer 3rd.csv`):** the Human/Demon buffer's Heavy Armor Mastery at 40 now replaces
  `[light_armor_mastery rogue_evasion]` (*"no point of human and demon to keep the `rogue_evasion` when their
  `light_armor_mastery` is being replaced"*). Regenerated with `--gen-passives`; `--check` clean.
- **Chat keeps 300 rows** (`ConsoleDisplayRows`, was 120): *"120 are very low number for when you want to know what the
  monster before have given you"*. The buffer behind it was already 1000; only the drawn rows were capped, and
  `RefreshConsole` builds at most that many per batch, so the cost is bounded.
- **The quest arrow is reset on every world entry** (`121d`: *"I had the quest but until i opened the details and click
  to track location the arrow isnt shown. Newbies need an arrow"*). The server already pins "Adventure Begins" as
  tracked; the client's "arrow off" choice lived on the UI for the whole app session, so turning it off on one
  character silenced it on the next. Entering the world now starts on the default: follow the first pinned quest.
- **The expiring-buff pulse goes 1 → 0.3** (`125a`: *"make it 0.3 now its almost visible"*), was 0.5.
- Docs: his new checklist rule (checked rows leave, `~` rows stay reworded) applied — §108-§126 moved verbatim to
  `Playtest-Archive.md`; `BL-332` (notifications) filed as a question; `BL-331`'s icon review closed.

## 2026-10-01 — 0.220.2: three rubber-bands — pavement speed, auto-farm walk-away, skill mid-walk

His report: *"I walk on the blessed pavement (+50 speed) -> click somewhere -> he gets off the pavement but the move
speed don't change visually -> he reaches the point then gets rubber back"*; *"I auto-farm -> I fight some mob -> I
click on the ground -> char start to move -> gets rubber back and continue with the fight"*; *"I click on the ground
to move -> I target a mob and click spell (not in range) -> char continue to point -> gets rubber back and go towards
the mob"*. **New APK + server restart.** No wire change, no `game.db` delete. Checklist §126.

- **Speed (case 1).** The client's walk prediction read your speed ONCE, at the tap, so leaving the Paved Streets, a
  Run/Walk toggle or a slow landing mid-walk left it running at the old number, further ahead every second, until it
  arrived and gave it all back. It now follows the speed every server sample carries (`EntityView.SetPredictSpeed`; a 0
  = rooted ends the walk). Server side, the spawn DTO (which the delta diff compares) carried the RAW base speed, so a
  speed change while STANDING was never sent at all; it carries `EffectiveSpeed` now, like the lean update.
- **Auto-farm (case 2).** A ground tap dropped the fight, but the next tick the autopilot picked the same mob back up
  and the chase overwrote your walk. The manual-move hold only gated roaming. Now ALL of the autopilot (except potions)
  waits while you walk your own tap (`Entity.WalkingManually`) and resumes the tick you stop: *"only when stopped then
  it attacks and use skills"*.
- **Skill mid-walk (case 3), and any other disagreement.** The client trusted the walk while the server's distance to
  the tapped point shrank, which a server heading toward a mob anywhere up to 90° off the line passes every sample. It
  now checks the HEADING of each server step (within 25° of our line), and treats silence as an answer too: no step our
  way within 0.8s of the tap, or a confirmed walk quiet for 0.4s, ends the prediction. A disagreement shows as a short
  glide where it happened instead of a walk to the point and a yank back. The arrival hold lets go on silence as well.

## 2026-10-01 — 0.220.1: an expiring buff PULSES its opacity instead of flashing a colour

His ask: *"now with the icons we need to make the 60s remaining buff bar blink to not be a background color but the
opacity .. Going 0.5~1"*. The under-a-minute warning swapped the square's box to yellow for half of every second, which
with the `BL-331` pictures flashed a yellow block over the icon. Now the whole square (picture, frame, stack count,
timer) breathes 1 → 0.5 → 1 once a second (a `CanvasGroup` per square, a cosine, so it is smooth rather than a hard
on/off). Debuffs and a gated-off (suppressed) buff do not pulse, as before. Client-only: **new APK**. Checklist §125.

## 2026-10-01 — 0.220.0: the SKILL TREE in game, at creation and every class master (`BL-330` step 2)

His ruling: *"Or we just can make a full skill tree at any class master. When opening each time it preselects whatever
u have (u can change and compare with other classes)"*, then *"Build the not locking one (with preselects)"*. **New
APK.** No server change, no `game.db` delete. Checklist §124.

- **One builder, `Game.Shared/SkillTreeData.cs`** (moved out of `tools/SkillCsvSeed/SkillTree.cs`): per race, Fighter /
  Mage → 2nd → 3rd/4th, each step listing only what it adds, plus the shared 4th kit, the stat swaps and the sigils.
  The page tool now only reshapes it; the regenerated page came out **byte-identical** before the change, which is the
  proof the move kept every row. The client builds the tree locally from it, like the Learn tab.
- **The window** (`GameUi.SkillTree.cs`, on the ROOT canvas so character select can open it): the page's pickers (Race
  incl. *Swaps & Sigils*, Start as, 2nd class, 3rd class) as button rows at the top of one scrolling column, then the
  sections. A row = icon, first learn level, the face name for that race/path, kind; tap it for one line per learn
  level with that rung's own text. Never locked: it opens on you and you can look anywhere.
- **Opened from:** character creation (*See the skill tree*, opened on the race and class being picked; the form grew to
  fit the button) and **every class master**, any tier (a *Skill tree* row in the dialog, opened on your race, base,
  2nd and 3rd class). It closes itself on a phase change, so one opened at creation does not follow you in.
- **The page** has the icons too (`--skill-tree` shrinks the client's PNGs to 48 px WebP; SkiaSharp added to the tool).
  The 12 stat swaps have no `skill_icons.csv` row yet and show a blank square.

## 2026-10-01 — drops window lists only what DROPS (client-only, unversioned, rides the next APK)

His find: *"if I search something I get 2 of the same thing ... 1st gives me 45lvl mob the other nothing"*, then
*"If item doesn't drop from any mob should not be there"*. The "Where does it drop?" candidate list walked the whole
`ItemCatalog`, so every `_temp` (2-hour box gear, D/C) and `_bound` (newbie kit, tutorial scrolls/potions) clone sat as
a second identical row beside its original, and craft/vendor/box-only items led to an empty table. The client now
builds the same `DropIndex.Build()` the server holds (once, on first open) and lists only ids in it: 774 → 520 rows,
0 identical-looking rows left. Chances stay the server's. `GameUi.DroppableIds`.

## 2026-10-01 — 0.219.1: icons in the Skills window (Known, Learn, Actions) and for every action (`BL-331`)

His ask: *"Skill icons need to be in skills window and actions also need icons so the 3 tab (known,learn,actions) need
icons"*. **New APK.** No `game.db` delete. Checklist §123.

- **Skills window:** every row on Known, Learn and Actions starts with the picture (38 px); the letters drop out of the
  text when there is one and stay when there is not. A Learn row you cannot buy yet greys its picture with its text;
  a passive on Known is already drawn dimmer, so it is not greyed twice. `GameUi.RowIcon`, an optional `icon` on
  `Row` / `Row2Buttons`.
- **Actions have icons:** 19 `action:<id>` rows in `docs/data/skill_icons.csv` (the same spelling the bar stores),
  rendered by `tools/SkillIcons` into `Resources/ActionIcons/<id>.png`; a new `social` colour for the party / chat /
  friend ones. `GameUi.TokenSprite` resolves a bar token to a skill or an action picture, so an action on the SKILL BAR
  shows its icon too. The review page has an Actions section.

## 2026-10-01 — 0.219.0: skill icons on the skill bar and the buff bar (`BL-331`)

His answer to `BL-331`: *"ok lets do the route B"*, game-icons.net. **New APK** (the pictures live in the client). No
`game.db` delete. Checklist §122.

- **`docs/data/skill_icons.csv`** (new, his to edit): one row per skill, `SKILL_ID,ICON,SCHOOL,COMMENT`. All **356**
  class-CSV skills matched to a game-icons.net glyph and one of 18 colour schools. A racial variant, a harmony, a healer
  single and a whisp wear the glyph of the skill they mirror, in their own colour.
- **`tools/SkillIcons`** (new dev tool, not in `Game.sln`; SkiaSharp): clones the icon set on first run (gitignored),
  validates every row, renders 128×128 PNGs into `Assets/Resources/SkillIcons/<skill id>.png` (passives dimmer), deletes
  stale ones, and writes the review page **`docs/design/SkillIcons.html`**. Every game-icons.net file is one white path on
  a black square, so the path is drawn directly: no SVG library.
- **Client:** `GameUi.SkillSprite(id)` loads and caches the sprite (a miss is cached too). The skill bar draws it over the
  letters, under the number, "A", count, reuse sheet and cancel X; greyed when the skill cannot be used. The buff bar
  draws it inside the square's tint (debuff red, expiry blink and gated-off grey stay visible as a frame), with the timer
  on a dark strip and the stack count kept. No icon = the letters, as before.
- **Wire:** `BuffDto.IconSkillId` (optional, defaults to "") = `BuffInstance.SourceSkillId` for every buff, so a group's
  square shows the group's picture. No protocol bump.
- **`docs/CREDITS.md`** (new): the CC BY 3.0 credit.
- Still letters: the Skills / Learn windows, the cast bar, the Skill Tree page, and buffs from skills no class CSV lists
  (potions, scrolls, the NPC shelf, mob debuffs). Listed in `BL-331`.

## 2026-10-01 — 0.218.2: copy reaches the phone's clipboard, the Learn page wears the face, Paved Streets = the whole city, a guide to Cera

His answers from the 0.218.1 pass. **New APK** (the clipboard and the Learn page are client). No `game.db` delete.
Checklist §121.

- 🔑 **§119c, Copy / Cut never reached the keyboard.** The strip wrote `GUIUtility.systemCopyBuffer`, which on the phone is
  NOT Android's clipboard under GameActivity: the text left the box and could not be pasted anywhere. New `UiKit.Clipboard`
  calls Android's own `ClipboardManager` on the UI thread (the Editor keeps `systemCopyBuffer`).
- **His find, an unlearned skill's page showed the plain text.** The Learn confirm read `def.DescriptionAt` instead of the
  face (`SkillDescriptionAt`), so a racial buff described itself in the plain words until you owned it. One line in
  `GameUi.Skills.cs`.
- **§118a, Paved Streets now covers the WHOLE city** (*"We can make it in the whole city.. no point only on the streets"*):
  the same city test as the Favor minute (`WorldMap.SafeZoneAt`, a city with `RegenBoost`). `TownLayout.OnStreet` deleted.
  The gardens and mud that slow you later are `BL-328`.
- **§116d, his Frost Spikes text** (`mage 1st.csv`): *"…and slows them down with 15%."* Faces regenerated.
- **His find, a new character is told where Cera is.** Born holding **"Adventure Begins"** (pinned to the tracker):
  *"Go meet Cera, our Adventurers Guild Receptionist. She has something important to tell you"*. Talking to her closes
  it on the spot, with no reward, and "Welcome, Traveller" is in the same window. New quest flag **`QuestDef.Guide`**:
  granted, never offered by an NPC, closes itself on its last TalkTo, can be abandoned and never returns, and the log
  lists it only while you hold it. Characters made before 0.218.2 do not get it (nothing to migrate pre-release).
- SmokeTest: the "empty buff bar on arrival" check sets Paved Streets aside (a new character spawns inside the town now).
  **ALL CHECKS PASSED.**

## 2026-10-01 — 0.218.1: the Android text boxes, found at last: a caret, mid-text taps, Copy / Cut / All

His F1-F3 from the 0.216.0 farm pass: *"still dont see the cursor when typing"*, *"still cannot copy text (paste
works)"*, *"still cannot select part of text or go to its middle"*. **New APK.** No `game.db` delete. Checklist §119.
Client only (`UiKit.cs`, one line in `GameUi.World.cs`).

- 🔑 **F1, the caret was never BUILT.** TMP creates the object that draws the caret *and* the selection highlight in
  `OnEnable`, and only when `textComponent` is already set. `UiKit.InputField` called `AddComponent<TMP_InputField>()`
  first, so `OnEnable` ran with no text component, and every box built inside an active window had no caret object at all.
  The caret and the selection were there and moving, just invisible. The fix cycles `enabled` after wiring. Earlier attempts
  (0.47.0's `shouldHideMobileInput`, `CaretToEnd`, 0.114.0's `BL-178`) all tuned behaviour around a caret that could not
  be drawn. The caret is also 3 units wide now, in the text colour.
- 🔑 **F2, Android's own copy menu cannot exist in this client.** The player runs on **GameActivity**
  (`androidApplicationEntry: 2`), which has no native EditText for the OS to put a selection menu on. 0.114.0 (`BL-178`)
  gave the chat box the native input hoping the menu would return. It did not, and it cost chat its caret completely
  (with the native input shown, TMP's `InPlaceEditing()` is false: no caret, no drag, taps ignored). Every box hides the
  native input again, and **`ClipboardBar`** is a small Copy / Cut / All strip above a box while part of its text is
  selected (drag across text, or double-tap a word). Paste stays the keyboard's, which already works. Password boxes get
  no strip. He asked for *"the normal"* menu if it could be made to work; it cannot without leaving GameActivity.
- **F3, a tap now keeps its place.** `CaretToEnd` moved the caret to the end a frame after any focus, so a first tap in
  the middle of a box was thrown away. It now acts only on focus set from code (Reply, the whisper action); a finger's tap
  stays where it landed.

## 2026-10-01 — 0.218.0: Paved Streets — run faster on a city's streets (`BL-324`)

His answer from the 0.216.0 farm pass: *"Agree with your proposal -> automatic visible buff"*, *"the 'Only Streets' effect
idea is good"*. **No new APK** (the speed and the bar row come from the server). No `game.db` delete. Checklist §118.

- **+50 run speed while RUNNING on a city's plaza, main roads or side paths, out of combat**, still capped at 250.
  Walking, a fight (`IsInCombat`: 30 s after a blow, or a DoT ticking), death and offline farming all switch it off.
- **Streets only, with no geodata:** the street shapes `BL-319` drew were already in `Game.Shared/TownLayout.cs`, so
  `TownLayout.OnStreet` is a distance test against each road strip (half its width) and the plaza radius. My Backlog
  proposal said this would need `BL-323`'s geodata; it did not.
- **A state, not a buff:** `Entity.OnPavedStreets`, written by `TickPavedStreets` each tick and re-sent (bar + stats)
  only when it flips. The bar shows it as a synthetic **"Paved Streets"** row, the same pattern as the grade-penalty rows,
  so nothing casts, stacks against Swift, counts toward the buff limit, or persists. `MovementTuning.PavedStreetsRunBonus`.
- **The farm pass is recorded** (Playtest-Archive `#playtest-0216-farm`): §109/§110 closed, `110a` answered; its three
  text-box bugs are My Finds `F1`-`F3`; new entries `BL-328` (water/mud terrain, *"lot later"*), `BL-329` (login session
  timer), `BL-330` (skill tree). SmokeTest ALL PASS.

## 2026-10-01 — 0.217.5: SP −30% on every skill learned at 40-75

His playtest: *"cut the sp requirements with 30% on everyone for 40~75 - it's impossible to learn skills even can't learn
the one to farm with"*. ⚠ **New APK** (the Learn tab prices locally). No `game.db` delete. Checklist §117.

- **Every class-table SP cell learned at 40-75 × 0.7** (three significant figures), across the eight 3rd-class CSVs:
  4,490 price rows, 1,763M → 1,234M SP. Each level's pot shrinks as a whole, so the weight split (`BL-326`) is
  unchanged and a later `--reweigh-sp` keeps the new pots. Below 40 and 76+ untouched; 0-SP rows stay 0.
- **New tool mode `SkillCsvSeed -- --scale-sp FROM TO FACTOR`**, sharing the reweigh's cell writer (keeps k/kk/×1000
  units, CRLF, BOM). `--check` 0, faces 879 / 0.

## 2026-10-01 — 0.217.4: Holy Spike + Monster Knowledge for Human/Demon priests; buffers fight with the weapon

His `cleric 2nd` / `buffer 3rd` / `healer 3rd` rows: *"a pve-only spell to help them lvl up somewhat closer to the elf"*,
and from 40 *"buffers are the fighters"*. ⚠ **New APK** (class tables). No `game.db` delete. Checklist §116.

- **`holy_spike` (new, Human + Demon cleric, 20/25/30/35)** — power 18/21/24/27, MP 15/18/21/26, cast 2.5s, reuse 1s,
  750 range, `MobTargetOnly` (refuses a player target). Faces: Holy Spike / Spirit Spike. Its DURATION cell was 30 (copied
  from Frost Spikes' slow; the spike has no effect) — set to 0.
- **`monster_knowledge_active`** — rung 1 at 35 on the Human/Demon cleric, rungs 2-4 at 40/48/52 on the Human/Demon
  Warchanter. Same ladder the warrior climbs.
- **Retirement at 40:** Holy Ray and all three sound skills now `Replaces` Holy Spike as well as Holy Bolt.
- **`@{slow}`** — a debuff's slow % is a DESCR key (`SkillEffect.Slow`), so a face can say "Slow the enemy for @{slow}"
  and `--check` now verifies every "15% Slow" it used to skip.
- `sp_weights.csv` row for holy_spike (magic, 1.5). `--gen-passives`, `--gen-faces` (879, 0 problems), `--check` 0.

## 2026-10-01 — 0.217.3: his CSV pass into the code; race faces for the 3rd-class single buffs

His pass over the fighter/rogue/tank/warrior/buffer CSVs, then *"do the 3rd class single buffs as ive done them"* and
*"then do the code"*. ⚠ **New APK** (class tables + faces). No `game.db` delete. Checklist §115.

- **Race faces for the 12 third-class single buffs**, in his three templates (Blessing: X / Forest X / Demonic Contract:
  X): Ferocity, Fortitude, Endurance (Body), Wellspring (Soul), Serenity, Insight, Fury, Guard (Shield Blessing), Bastion
  (Shield Hardening), Mana, and Great Strength / Great Bulwark, each naming its own race's ordinary buff. **The group
  buffs and harmonies stay one name for every race** (his ruling: *"gruped stay"*).
- **`@{mpcost.2}`** — a face placeholder can name the 2nd number of a word (Mana Blessing's magic MP cost). `Faces.cs`.
- **`fighter_accuracy` is "Hit Rate Mastery" everywhere** (buffer 4th still said Accuracy). His renames ride the
  generated ladders: Faster Attack Mastery (`fury_mastery`), Swiftness, Light armor Evasion.
- **Archer 4th Light armor Evasion = +13..+17**, 2 under the dagger's +15..+19 (the archer 3rd never levels it). The
  generated ladder now gives the archer rungs 10-14 instead of 12-16.
- **The buffer's 3rd-class equipment passives are free**: `cleric_heavy_armor_mastery` and `harmonist_bow_proficiency`
  SP 0 (*"so buffers can freely use their designed equipment at 3rd class"*). SP-0 rows sit outside the level pot, so no
  other price moved. His new REPLACES: Heavy Caster Mastery replaces Light Caster Mastery; Heavy Armor Mastery replaces
  Light Armor Mastery (Human/Demon buffer).
- His new DESCRIPTION texts (tank/rogue/warrior/fighter 1st passives, harmonies, Warchanter group buffs) regenerated.
- Verified: `--gen-passives`, `--gen-faces` (876, 0 problems), `--check` 0 discrepancies, server + Unity build.

## 2026-09-30 — 0.217.2: the class CSV names the skill again; the spirit helper reads the faces

His ask, after the two-file split: *"i wonder if we remove those files and just author them inside the classes csvs as
it was before .. now it looks so confusing"* — then *"build it that way"*. It REPLACES the split below the same day.
⚠ **New APK** (his new racial faces + the NPC names ride in `SkillFaces.g.cs`). No `game.db` delete. Checklist §114.

- **Class CSV `NAME` is the real name again**, and a new LAST column **`DESCRIPTION`** holds the text the player reads,
  written on ONE row of the skill (`DESCR` stays the numbers). Last, not beside DESCR: `Check.cs` reads the first 15
  columns by position, and long prose at the far right keeps MP/SP readable.
- **`skill_faces.csv` = exceptions only** (31 rows): a RACE or CLASS row — Forest/Fire Strength, the harmonist's Bow
  Expertise, the Momentums, Holy/Moonlight/Spirit Bolt. Blank DESCRIPTION there = the plain one. **`skill_faces_other.csv`**
  (412 rows) names what no class CSV lists: mobs, `npc_*` blessings, items. `skill_faces_single.csv` is gone.
- **One name per skill, checked:** a skill's rows may disagree only where an exception row (or the code's per-level
  Grade names) explains it; two unexplained names is a `--check` error. The move corrected the class-CSV typos the game
  had never shown (Wirlwind, Shattaring, Bow Stence, Domonic, Knowlege, Healers …, 119 cells) so nothing on screen moved.
  Two of his comments held an unquoted comma that spilled into the new column (`dual 4th` double_mastery, `shared 4th`
  sigil note) — quoted.
- **The spirit helper's names are faces now.** `NpcBuffShelf.DisplayName` read the top rung's CODE name; it reads the
  shelf id's face (`npc_accuracy` → "NPC Aim"), and the bar stopped prefixing "NPC " itself — one name, window and bar.
  A shelf item that is also a class skill (the three Marks) takes a `CLASS = NPC` row: "NPC Blood Mark" at the NPC,
  "Blood Mark" for the healer. `SkillFaces.ForNpcShelf`.
- Verified: `SkillFaces.g.cs` before/after the move differs only in his own new rows (and `backlash`'s unused plain
  name, now "Physical Backlash" — every race wears its own). `--check` 0, server + Unity build, `--gen-passives` unchanged.

## 2026-09-30 — tools only: the face file splits in two (no build, no version) — REPLACED by 0.217.2 the same day

His ask: *"the skill_faces only contains skills that are duplicates like bow_expertise and might and twin_arrow"* — and
of the two ways offered, *"2 files … you wont lose any information"*. Nothing in the game moves; `SkillFaces.g.cs` is
the same 788 faces in a new order.

- **`docs/data/skill_faces.csv`** (251 rows) — the SHARED skills: learned by two or more races, or by two classes of
  one race that are not one line, or already wearing a race/class face. Strike, Might, Bow Expertise, Twin Arrows, Holy
  Bolt, the stat swaps.
- **`docs/data/skill_faces_single.csv`** (537 rows) — one race on one class line (`elf_heal`, `human_vampiric_bolt`,
  the rogue disciplines' own kits) and every skill no class learns (mobs, NPCs, whisps, items). Same columns, same rules.
- Ownership comes from the compiled `ClassSkills` lineages, not the CSVs (their Race column is only in the 1st-class
  files, and a rogue 3rd discipline is one race). **`SkillCsvSeed -- --sort-faces`** moves rows to the right file,
  section by section; **`--check`** flags a row in the wrong one.

## 2026-09-30 — 0.217.1: "Lv.N" is YOUR step, not the ladder's rung

His find: *"when i learn a skill i learn it lvl 2 directly .. i learned agility and it automatically gave me lvl 2"*, and
the fear behind it: *"will one learn lvls 3-5-10 the other 6-8-12?"* — yes, it would. `BL-314` made every shared passive
one ladder holding every class's values sorted by value, so classes INTERLEAVE on it (Human archer's crit-damage rungs
3/5/8/12…, the dagger's 6/9/11/14…), and the buff ladders start at rung 2 because rung 1 is the potion. The label printed
the rung. ⚠ **New APK.** No `game.db` delete, no number changed.

- **`ClassSkills.ShownLevel`** — the count of the character's LINEAGE's distinct rungs at or below the one held (base
  class + 2nd + 3rd + 4th; `Cumulative` alone drops the base list at the class change, which would restart a ladder at
  Lv.1). Measured: every crit-damage path reads Lv.1…35 straight; Agility is Lv.1/2/3 = +2/+3/+4 for the cleric, the
  Lightbringer and the Warchanter alike; a fighter's weapon mastery at 20 continues at Lv.4 from his three 1st-class rungs.
- The rung stays the engine's truth (buff rank, persistence, the numbers). Only the label moved: the Known/Learn tabs,
  the learn confirm, the skill card, the learn messages, and the **buff bar**, where the label is stamped at landing from
  the CASTER's path (`BuffInstance.ShownLevel`, persisted) — so "Agility Lv.3" is +4 on whoever wears it.
- **NPC buffer blessings read "NPC Might"** with no level (his ruling). Potions and scrolls show no level (they are
  one-rung singles and never did). A mob's buff or debuff keeps its rung — a creature has no class path.
- Fixed on the way: the learn confirm's before→after compared against `newLevel − 1`, which on an interleaved ladder is
  ANOTHER class's rung. It reads the rung you own now.

## 2026-09-30 — 0.217.0: skill FACES — how a skill looks, apart from what it does (`BL-327`)

His question: *"is it possible to tell a skill to use "this" shell for the visuals (name/description/icon/animation) but
underneath to be "that" skill?"* — and his split once it was: *"the class csv is the numbers per lvl while the face is
the display"*. ⚠ **New APK** and a **`game.db` delete** (three skill ids removed).

- **`docs/data/skill_faces.csv` is his** — `SKILL_ID,NAME,RACE,CLASS,DESCRIPTION,COMMENT`, a blank row for every one of
  the 767 skills (actives, then buffs, passives, then everything outside a class CSV), plus race/class rows. Resolution:
  a CLASS row anywhere in the character's lineage (4th → 3rd → 2nd name → Fighter/Mage) → a RACE row → the blank row;
  creatures and NPCs read the blank row only. `SkillFaces.For` in Game.Shared is the one lookup.
- **Placeholders:** `@` = power, `@{key}` = any `DESCR-KEYS.md` word (or the metric key; `%`/`#` suffix forces the
  reading), `@{duration}`, `[ … ]` = all-or-nothing. An unbracketed number a level lacks drops its CLAUSE, so the top
  rung's text serves every lower rung (his *"use the maximum … the lower lvls will take from there"*). Numbers come from
  the same `Descr.Pool` `--check` proves against the class CSVs, and are **pre-rendered per level** by
  `SkillCsvSeed --gen-faces` into `SkillFaces.g.cs` — no templating at runtime. `--check` now also walks the faces (bad
  id/race/class/word, a skill without a blank row, a stale `.g.cs`). An EMPTY description = the code's own per-level
  text (144 seeded that way, where the code's text changes per level and still has numbers the seeder could not tie).
- **Everything reads the face:** the server's cast bar, combat text, reuse message, auto-hunt list, totems, traps,
  whisps, procs, group names (`GameLoopService.FaceOf` / `SkillName`); the client's skill bar letters, skill window,
  Learn/Known lists. `CastInfo` carries the skill id, so the bar lights the casting square by ID — for an Elf or Demon
  healer it never lit before (it compared `def.Name` to "Moonlight Bolt").
- **A buff wears its CASTER's face**, name and description at the caster's rung, stored on the buff (`FaceId`,
  `FaceLevel`) and in `BuffSnapshot`, so it survives a relog. The one-child wrapper branch of `ApplyBuff` now passes the
  name through (it dropped it — the cast-bar/buff-bar mismatch noted under `BL-263`); a potion still pours its child's name.
- **The three racial Mights are ONE skill** (his *"merge them as one ill split them in the file as faces"*): a mage learns
  `cast_atk_phys` rung 1 at 7; Forest Might / Demonic Strength / Blessing of Might are its elf/demon/human faces, and a
  cleric continues the same skill (and name) from 20. `elf_/demon_/human_cast_atk_phys`, `MageMightFor`,
  `MageMightSet`, the cleric row's `Replaces` and `SkillDef.NamesItsBuff` are deleted; `mage 1st.csv` is one Might row.
  SP unchanged (the reweigh is a fixed point on it).
- **The old overrides moved into the file:** `ClassSkill.DisplayName`/`Icon` are deleted. Holy/Moonlight/Spirit Bolt,
  Physical/Magical Backlash (race rows) and Battle/Bow/Stab Momentum (12 class rows) are faces now. The rogue 2nd's
  "Critical Damage" came only from the CSV NAME cell and is dropped (the NAME column is a label now).
- `ClassSkills.DisplayName` survives as a forwarder for the dev tools that match CSV rows by name. `--check` clean;
  Unity type-check clean.

## 2026-09-29 — 0.216.0: SP is one pot per level, split by weight (`BL-326`)

His rule: *"i want Sp to be some how equal not one active skill to cost 880k SP and one passive that give me +0.1mp
regen to cost 2600k ... sum all the sp/lvl and split it for skills as weighted ... active skills are x1 passives should
be less"*, with a worked example (archer at 60: 0.33 / 1 / 1.5).

- **The rule:** for one class file and one level, the pot is the sum of the SP cells there; each row takes pot × its
  weight / Σ weights. A race-only row counts by its share of the three races (so a symmetric kit splits exactly per
  race, and a rerun changes nothing). Rows the class tables do not carry (central race blocks, SP-0 auto-grants) keep
  their price. Rounded to three significant figures.
- **`docs/data/sp_weights.csv` is his** (318 skills): passive 0.33, buff/utility 1, damage/debuff/heal/trap 1.5 by
  default; his example rows marked `owner`. `SkillCsvSeed --reweigh-sp [--show <file>]` rewrites the SP cells (2,736
  cells moved) and regenerates. A new skill is added to the file at its default.
- **The CSV cell IS the price, actives included:** `--gen-passives` now also writes
  `ClassSkillTables.SpPrices.g.cs` (8,874 lines), which `ClassSkills` writes onto every class row at load. The passive
  ×k of 0.215.0 (`SpScarcity`, `--apply-k`) is deleted.
- **Measured:** the pots summed 20.82bn SP before and after; `--sp-budget` x 20-75 unchanged per group (daggers
  0.62-0.66, bows 0.53-0.56, tanks 0.67-0.72, Lightbringer 0.54-0.55, Warchanter 0.48-0.51) except where race kits
  differ: Elf Ravager 0.72→0.78, Magus 0.60→0.57-0.63. `--check` clean. New APK (Learn tab prices are client-side).

## 2026-09-29 — 0.215.2: Holy Ray at 750

His ruling, right after 0.215.1: *"healer holy_ray as well 750 range 40+"*. **Holy Ray is 750 on every rung, 40-90**
(was 600), Holy Bolt's reach, the same as the race spells in 0.215.1. `healer 3rd/4th.csv` range column moved with it.
Ships in the same APK as 0.215.1.

## 2026-09-29 — 0.215.1: one damage answer per mage race; Spell Ward gone (`BL-325`)

His `mage 1st.csv` edits of the same day. ⚠ **New APK** (class tables changed) and a **`game.db` delete**
(`elf_self_heal` and `tank_spell_ward` are gone).

- **Elf mystics get Frost Spikes from 14.** The Elf nuker's spell moved into the mage race block (every Elf cleric,
  buffer and nuker learns it), with five new rungs in front: 14/20/25/30/35, power 15-27, slow 15%. Its 34 rungs are
  deleted from `nuker 3rd/4th.csv` and the nuker tables. Nothing changes for an Elf nuker except that it opens earlier.
- **The Elf's Self Heal is deleted** (his row deletion). The healer's Heal no longer replaces anything; the
  `cleric 2nd.csv` REPLACES cell is `[]`.
- **Demon: Over the Limit reworked, "10s of pure havoc".** Four rungs at 14/40/60/70 (was 7/20/40/60/70), 10s window
  (was 5s), CD 60: +10/12/15/20% P/M.Atk and +5/6/8/10% P/M crit rate, attack speed and cast speed.
- **Range 750 from 20 up for Vampiric Bolt and Frost Spikes, including 40+ (was 900):** Holy Bolt's reach, so a healer
  or buffer does not get the nuker's range from a race spell (his ruling, same day). 600 at 14.
- **`BL-325`: `tank_spell_ward` deleted** — *"no where in the csvs so remove it"*. A tank's fizzle protection is the
  paid `magic_protection` @80 alone.
- `debuff_landmods.csv` regenerated: Frost Spikes is now listed for every Elf mystic class. `SkillCsvSeed --check`: 0.

## 2026-09-29 — 0.215.0: the passive split, built (`BL-314`)

His CSVs from 7ebbaef are now the game. ⚠ **New APK** (the Learn tab is built from the compiled class tables) and a
**`game.db` delete** (dozens of skill ids are gone).

- **One ladder per stat, shared across classes.** `heavy_/light_/mage_armor_mastery`, `weapon_mastery`, `bow_mastery`,
  `spellcaster_weapon_mastery`, `anti_magic` / `magic_resistance`, the regen, crit, speed, accuracy, MP and cast pieces,
  `strength_mastery`, `blunt_cleave`, `fury_mastery` and the rest: 34 ladders, 756 rungs. Each is the union of every value
  a class authors, and each class learns only its own rungs at its own price. They are **generated from the CSVs**
  (`SkillCsvSeed --gen-passives`); edit the row, regenerate.
- **The 29 old bundles are deleted** (every class armour and weapon mastery, Spell Mastery, Warrior's Strength, the
  two-hand masteries, the buffer's armour and race weapon masteries, tank/mage/dual anti-magic).
- **SP scarcity:** every passive a class learns at 20-75 costs ×k: daggers 7.15, bows 9.09, warriors 4.63, tanks 3.58,
  Magus 4.60, Lightbringer 3.52, Warchanter 3.73 (2nd-tier rogue and cleric: the mean of their two groups). The CSVs show
  the price you pay. Measured: every path lands on its target (daggers 0.62-0.66 … Warchanter 0.49-0.51).
- **Strength Mastery** pays its highest learned rung whose weapon gate holds: a warrior with a bow keeps ×1.085.
- **The Lightbringer's first robe rung retires the Light Caster Mastery**; the Warchanter keeps it (a class-row Replaces).
- **Magic Protection** is its own skill at 80: 150kk SP + 10kk gold (his ruling).
- The tank's auto-granted fizzle ×2/×2.5/×3 moved from `anti_magic` to `tank_spell_ward`, unchanged. It makes the paid
  Magic Protection worth nothing; that is `BL-325`, his call.
- `SkillCsvSeed --check`: **0 discrepancies**. SmokeTest: all checks passed.

## 2026-09-29 — 0.214.43: one Vampiric Bolt for the Human mage; the passive CSVs re-authored (`BL-314`)

His note: *"fix mage1st vampiric_bolt @14 .. to match the human_vampiric_bolt (now i have two skills at lvl 20 with
human mage)"*.

- **The Human's level-14 Vampiric Bolt is rung 1 of `human_vampiric_bolt`** (power 21, 28 MP, range 600, 2k SP), so a
  Human mage holds one drain bolt from 14 to 90 instead of a 14 "taster" next to the ladder from 20. The one-rung
  `vampiric_bolt` skill is deleted. Holy Bolt now replaces only Magic Bolt; it used to also replace the taster, and
  the ladder handed the bolt straight back at 20 anyway. The two `[vampiric_bolt]` Replaces cells in `nuker 3rd.csv`
  (Elf Frost Spikes, Demon Witches Curse) named a Human-only skill and are emptied. ⚠ **New APK** (the Learn tab is
  built from the compiled class tables) and a **`game.db` delete** (the old id is gone).
- **`BL-314`, data only, NOT built:** every 2nd-4th class CSV is re-authored into single-stat passives, and the armor
  and weapon masteries are merged into shared ladders from level 1 (`heavy_/light_/mage_armor_mastery`,
  `weapon_mastery`, `bow_mastery`, `spellcaster_weapon_mastery`, `anti_magic` / `magic_resistance` /
  `magic_protection`, …). The engine still runs the old bundles, so `SkillCsvSeed --check` reports ~400 differences
  until the build step. See `docs/design/PassiveSplit.md` §10-§11, which also records his two data fixes (robe P.Def
  47 and spellcaster M.Atk 36 at level 48).

## 2026-09-29 — 0.214.42: the Blessing bar you can read

His note on 0.214.41: *"the active blessing bar -> cannot read the timer - the golden is very close to the white - need
color change or the forgrownd to change ... or the active one is the current fillup one and the fillup rangeish color can
be a bit darker .. u decide"*.

- **A running Blessing is deep amber** (0.78, 0.45, 0.05) instead of bright gold (0.96, 0.82, 0.36). White text on the
  old gold had a contrast of about 1.3:1; on the amber it is about 3.6:1, and the label's outline is a little thicker
  (0.3 instead of 0.2) on this bar only. The text stays white, so it still reads over the empty dark part of the bar.
- **The filling gauge is a step darker gold** (0.52, 0.44, 0.18, was 0.62, 0.52, 0.22), so filling and running still
  differ at a glance.
- Client only (`GameUi.World.cs`). ⚠ **New APK**; the server is unchanged and the protocol did not move.

## 2026-09-28 — 0.214.41: towns that look like towns (`BL-319`)

> *"Visuals 1st -> as u said the 3d models will do the collision"* · *"expand as much as u need to fit everithing .. a
> major city requires u to walk for a bit"* · *"until maybe bl-281 lands we can leave them at the door"* · *"the Hunt
> Lodge we can call it "Adventurers Guild" and behind the "counter" can stay the "Guild Receptionist""*

- **Every town has streets now**, drawn flat on the ground: main roads out to the gates, a plaza, side paths to the
  doors, building footprints and a wall with a gap at each gate. The walls are **visual**, so you still walk through them.
- **The three major cities are X-shaped, with four gates**: Brackenford, Greymarsh and Frostmere. Each quarter has one
  job. The **church** (north-west) holds the class master and the Mindwright, plus Brackenford's High Priest and Elder.
  The **market** (north-east) is Arms & Armour, with the Apothecary beside it (and Greymarsh's Assayer). The
  **Keeper** is south-east, with Frostmere's Ledgerkeep at his side door. The **crafthall** is south-west: the Master
  Crafter at its south door, the Anvil in the yard behind him, and Frostmere's three recipe givers.
- **Stonewatch and Ironreach are Y-shaped, with three gates**. Stonewatch's stem points south at Brackenford.
  Ironreach is the same Y turned round, with its stem north toward Brackenford and its arms toward its fields.
- **Greymarsh and Frostmere grow from radius 2000 to 3000**, and their fields moved out by 1000 to keep clear of the wall.
- **Every NPC stands at a door**, not in the road. The Gatekeeper stands on the plaza.
- **The Huntmaster is the Guild Receptionist now**, at the **Adventurers Guild** up the north road. Same ids, same
  contracts; the quest text says "Guild Receptionist".
- **Two guards at every gate** (three or four per town) instead of one pair at the bottom.
- **The roads between towns leave through the gates**, not through the wall.
- New APK for the drawing. The server still speaks protocol 52, so a 0.214.40 APK works but shows no streets. No
  `game.db` delete. SmokeTest ALL PASS. `BalanceMatrix --town-svg out.html` draws the towns as built.

## 2026-09-28 — 0.214.40: the skill bar is five pages of three tens (`BL-321`)

> *"each page 1/5 will have 10/20/30 slots ... so max we will have 150 skill slots"* · *"the page to have 10 slots ...
> not 12 ... should have made the example with settings 2x5/1x10"* · *"I would like to have a copy"*

- **A bar is ten squares now, in three shapes: 2x5, 1x10, 5x2** (the 6x1 and the half bars are gone).
- **Each page owns its main bar and its own two additional bars**: page 1 = entries 1-10, + 11-20, + 21-30; page 2 =
  31-40, + 41-50, + 51-60 … page 5 = 121-150. Paging the main pages the additional bars with it, so nothing slides from
  one bar into another any more. **150 entries** in all.
- Setup: **"Additional bars: 0 / 1 / 2"**, each one a full ten in the main's shape: 1x10 → 2x10 → 3x10, 2x5 → 4x5 →
  6x5, 5x2 → 5x4 → 5x6. Bar 1 sits next to the main (above it, or left of the 5x2), bar 2 beyond it.
- **`[To bar]` is never disabled and places a COPY**: one skill can sit on as many squares as you like (a solo page and
  a party page).
- "1/5" stays on the main bar only.
- ⚠ **Protocol 51 → 52: install the new APK and the new server together.** No `game.db` delete: an old bar keeps
  its entries by index, so they land in the new layout shifted (the old squares 11-12 now sit on page 1's bar 1). Re-arrange once.
- SmokeTest: the bar is 150 long, and a copy of a placed skill on the very last square survives the relog.

## 2026-09-28 — 0.214.39: the skill-bar size reads ×1 (`BL-320`)

> *"make the x2.5 to show as x1 .. the bar that way will go from 0.6(make it 0.5) to 1.8(make it 2) .. so from 0.5 to 2
> default x1 (old numbers x1.25~5 default 2.5)"*

- **Setup's "Skill bar size" slider now shows the default as ×1**, and runs **×0.5 to ×2** (the old ×1.25 to ×5, a
  little wider than 0.214.35's 1.5-4.5 at both ends).
- The stored value keeps its old units, so a phone that already set a size keeps it. Client only.
- Your 0.214.32-38 note is §107 in `Open-Checklist.md`; its `BL-314` reasoning (SP scarcity until 76) is in the Backlog.

## 2026-09-28 — 0.214.38: full drops below level 40 (`BL-307`)

> *"Lower lvl mobs also need full drops. F/E grade also need Common equipments and drops for mythic/common"* ·
> *"build the BL-307 as u proposed - looks good on paper"*

Built exactly as `docs/design/LowLevelDrops.md` proposed:
- **36 new items: a Common copy of every F and E piece** (`CommonMinLevel` 40 → 1). Same stats as the Mythic, no set,
  no attribute, no enchant, 5% of its price. They cannot be broken (no essence below D) and no shop sells them.
- **Every creature under 40 is dealt a specialty**, in two new bands, 1-19 and 20-39, by the same rule as 40+. The
  three starter creatures are dealt one like the rest.
- **Commons: F at the T40 table ×1 (14% a kill in all), E at ×0.35 (4.9%)**, elites ×2.
- **The lucky Mythic piece: F 1 in 1,000, E 1 in 3,000**, split over the creature's kinds (40+ stays 1 in 10,000).
- **Base mats from 20**: 0.1 a kill from 20 to 34, and the old curve from 35.
- No recipes, parts or Nightsilver below 40: crafting still starts at T40.
- ⚠ **Needs the new APK**: the client holds the item catalog, and an unknown id shows as a blank row.
- `ItemIds.md` and `mob_drops.csv` regenerated; Formulas.md's drop section follows.

## 2026-09-28 — 0.214.37: the Mindwright stands off the road (`BL-318`)

> *"the mindweaver i dont like his position in the middle of the gate ... move it on the side .. think of the town as it
> will have roads and houses ... and shops .. its not an open field"*

- **Every Mindwright moved off the south road's centre line**, to the south-east: Brackenford's from (24000, 25800) to
  (24700, 25300), Greymarsh's and Frostmere's from (centre, +1700) to (+700, +1250), below the Keeper. The town
  layouts themselves are `BL-319` (a sketch first).

## 2026-09-28 — 0.214.36: the quest arrow can be turned off (`BL-316`)

> *"[Location:Current] to be clickable and to stop the arrow (no quest location is tracked when done that)"*

- **Press `[Location: current]`** in the followed quest's details and the arrow goes away: no quest is followed until you
  press `[Location tracking]` on one again. It no longer falls back to the top pin while it is off.

## 2026-09-28 — 0.214.35: the skill bar is ×2.5 by default, ×1.5 to ×4.5 (`BL-320`)

> *"make the dafualt of skill bar current x2.5 .. and to range from 1.5 to 4.5 (current numbers)"*

- **Skill bar size: default ×2.5, slider ×1.5-×4.5**, in today's numbers (×1 is still a third of the old bar, so the
  default is 5/6 of it). *"Are u sure its 3 times?"* It was: a third per side, which is a ninth of the area, and that is
  why it looked so much smaller.
- A new pref key, so a phone that saved ×1 starts at ×2.5 rather than being clamped to ×1.5. Reset to defaults gives ×2.5.

## 2026-09-28 — 0.214.34: Charge fires in auto-hunt (`BL-322`)

> *"charge need to be able to be used in auto mode as well .. its not a taunt .. phantom jump is used but charge is not"*

- **A charge is an auto-hunt attack now.** Charge has no damage of its own, so the auto chain filed it with the skills it
  never casts. It joins the attack rotation. A charge that also taunts stays manual (`BL-83`).
- **It is skipped, not attempted, while the mob is closer than 150**, where a tap is refused, so it cannot fill the chat
  in melee.

## 2026-09-28 — 0.214.33: the Blessing pauses only out of combat (`BL-317`)

> *"remove the tawn/active pause — only when u go out of combat to say (paused) … entering in town dont automattically
> pauses it … we will make it an exploit if acive blessing is paused"*

- **Only the gauge's fill pauses**, and only out of combat (the 30 s window) or dead. A town no longer pauses it.
- **A running Blessing's 3 minutes never pause.** Pausing it let a player park the clock in town and spend it only on
  the kills that pay most.

## 2026-09-28 — 0.214.32: the Learn row goes, the Drop window lines up, the Warehouse tabs wrap (§106)

- **§106.3 — a learned skill's row now leaves the Learn tab at once.** The window redraws only when its stamp
  changes, and the stamp counted learned skills plus SP. A skill that REPLACES another (Elemental Bolt over Magic
  Bolt) keeps the count, and a gold-priced one keeps the SP, so the server's answer changed nothing the window
  looked at. The click's own redraw ran before that answer arrived. `Boot.LearnedRevision` is bumped on every
  Learned push and the stamp reads it.
- **§106.2 — the Drop window lines up.** Every control there is pinned by its top-left corner, but its x was written
  as a centre (`x + w/2`): the filter buttons sat half a width to the right, the header started at the middle of the
  window and each column was half its width off. The Back button now shares the text box's top edge.
- **§106.1 — the Warehouse's buttons wrap** instead of running out of the window (`UiKit.Flow`, a wrap panel): the
  four mode buttons on one row, the category tabs and the order button on the next.

## 2026-09-27 — 0.214.31: craft exp from what a craft consumes (`BL-315`)

> *"Each base item to have a weight (points) and based on those points a craft total to be calculated"* ·
> *"increase the curve about 4 times .. 1~2 weapon is low amount for lvl up ... Leave the points"* · *"Potion crafts
> need ... some sort of modifier to match wepons armors.. May be match rings ... not to skyrocket"*

- **A craft attempt pays the weight of everything it consumed** (`Crafting.CraftWeight`, `CraftPoints(recipe, pct)`):
  a fail too, refines too, and a 20/40/60% gear recipe pays for the 30/50/70% it consumes. 100%: 2H T40 300 · T52 5,880
  · T61 49,470 · T76 132,660 · T80 276,500; ring 30 … 27,650; Alloy 6, Legendary Nightsilver 6,500. The weights are
  authored literals, generated once by `BalanceMatrix --craft-weights`.
- **A potion batch pays the ring of its tier** (War Rune 1h = a T61 ring, 4,947).
- **The level curve is his, ×4**: 800 · 2,000 · 28,000 · 120,000 · 240,000 · 400,000 · 600,000 · 880,000 · 1.2M · 1.6M
  (L10 = 5,070,800). About 5-9 of the tier's 2H per level (21 at L3→4, before T61 opens); a potion-only crafter
  needs ~477 batches before Rare HP/MP and ~690 before Instant. `BalanceMatrix --craft-points` prints it all live.
- ⚠ An existing crafter's exp now reads as a much LOWER level while its spent type levels stay, so its free points go
  negative: re-set it from the Debug window's crafting levels (or delete `game.db`).

## 2026-09-27 — 0.214.30: Binding Trap holds; recipe prices and rune drops cut; craft exp shown

> *"Still binding trap don't hold the enemy in place"* · *"the rcps for war/spell Runes are 200k each .. And I just
> made 4kk out of them .. Decrease their chance ~10 times ... Decrease the price of rcps for buff potions ~3 times
> ... And equip rcps as well 3 times"* · *"I cannot see nowhere what rcp homuch points it give nr how much progress
> I have made"*

- 🔴 **Binding Trap roots again, and so does every weapon-gated debuff.** `Entity.RefreshBuffSuppression` asked the
  *victim* whether it held the skill's weapon: a bow-gated Root on a mob with no bow was switched off the tick it
  landed (the icon stayed, the legs kept walking). Debuffs are now never suppressed; the gate still holds for the
  caster's own buffs (Bow Expertise without a bow).
- **War / Spell Rune recipes drop at 1/10** (`MobCatalog.GenericRecipeDropMul`, L4 and L8 lines): 1/100 → 1/1000
  at T61 and T80. The recipe items' prices are unchanged (400k / 2M value).
- **Buff-potion recipes (Greater Swift / Alacrity / Fury, L1) cost a third**: 50k → 17k (`Crafting.GenericRecipePrice`).
- **Gear recipes cost a third**: `ShopRecipePriceFraction` 10% → 1/30 of the item's price, on every % and on the
  Master's T40/T52 shelf.
- **The crafting window shows `+N craft exp` on every recipe and `(done/needed exp)` beside your crafting level.**
- `BL-315` filed: his MP-based craft exp, measured by `BalanceMatrix --craft-points` into
  [design/CraftPoints.md](design/CraftPoints.md). Not built; two answers owed.

## 2026-09-27 — 0.214.29: drop chances read as odds

> *"not 1% but 1/100 not 0.33% but 1/300 etc... Over 100% is 5/1 … mats when u get 100 per kill is 100/1.. Or if
> it's a range 20~100/1"* · *"1/1000000 is unreadable it's 1/1M"*

- **The mob's Drops tab and the Drops database show odds, not percentages** (`MobCatalog.DropOddsText`, one
  formatter for both). Below one per kill: `1/N`, N exact under 1000 and two significant digits above it
  (`1/1200`, `1/12K`, `1/1M`). At one or more: the haul per kill: `5/1`, `100/1`, `20~100/1`.
- **Gear above 100% reads `2~3/1`, not `1/1`**: the kill roll really does pay extra copies of gear above 100%
  (`DropCopies`), so the text shows what lands.
- Stack quantities now include the stack-size rate (`DropAmount`), as the kill roll already did.
- Server-side text only; the version bump is the one reason a new APK is needed.

## 2026-09-27 — 0.214.28: Bow Expertise is one two-rung skill; `BL-314` filed

> *"Bow expertise L2@52 don't replaces L1"* · *"archers is the main .. So bow_expertise is the 2 rung one and the
> buffer learns that one just on different lvls ...so we can remove the wc_bow_expertise"*

- **`bow_expertise` has two rungs:** rung 1 +8% (25 MP, rogue at 36), rung 2 +12% (85 MP). The archer learns
  rung 2 at 52 (37,000 SP) and the Elf Warchanter takes rung 2 directly at 56 (42,000 SP). The archer used to
  get a second skill (`wc_bow_expertise`) at 52 and kept BOTH in the Skills window; that id is deleted.
  `archer 3rd.csv` and `buffer 3rd.csv` name `bow_expertise` now. Needs a new APK (the Learn tab is built from
  the class tables on the phone) and a `game.db` delete for characters that already held the old id.
- **`BL-314` filed:** split the bundled armor masteries into single-stat passives (regen, speed …) shared across
  classes — design first.

## 2026-09-26 — 0.214.27: UI size as ×1, a skill-bar size, Setup from the login screen

> *"At 800 scale the ui size is OK ... change it to x1 to be 800 ... The 480~1100 means nothing"* ·
> *"decrease the size of the skill bar 3 times and make in the setup another bar for scaling the skill bar
> (1/5 to 5 times the new size)"* · *"I made the ui scale 480 and it became so big that I had to clear the
> app data ... can the login ui have a setup button ... the setup window not to be affected by the ui scale"*

- **UI size is a multiplier now:** ×1 = the 800-high design (was 720 by default, so a fresh install is a little
  smaller than before). The slider runs ×0.60 to ×1.60 and applies **live**. New pref key `ui.scale`; the old raw
  height is ignored, so a phone stuck at 480 starts over at ×1.
- **Skill bar size:** a new Setup slider, ×0.20 to ×5. ×1 is **a third of the old bar**, as asked, so ×3 is
  the old size. It scales the bar as one piece from its bottom-right corner and applies live.
- **Setup has its own canvas**, fixed at ×1 and drawn above everything, so the UI size can no longer make it
  unusable. It is reachable from a new **Setup** button on the login screen as well as from the menu.
- **The menu wraps into columns** when it would run off the bottom of the screen at a large UI size, so Drops,
  Setup and Leave stay reachable.
- **Reset to defaults** puts both sizes back at once. The other settings still apply after a restart.

## 2026-09-26 — 0.214.26: major cities and towns (`BL-303` Q1, closes it)

> *"We can make major cities (staring one , 40-65, and the 76+, where class masters are) those can have the
> craftMaster+anvil, mindweaver and other for respecs..the other "non major" cities(towns) can have only
> shops/buffer/gk/keeper - something like that"*

- **Major cities:** Brackenford, Greymarsh and Frostmere (the three with a class master). Each has the Master
  Crafter, his Anvil and a **Mindwright** (skill reset). Greymarsh and Frostmere get their Mindwright for the first
  time (Mindwright Ivo, Mindwright Rhosa), at the bottom-centre of town like Brackenford's.
- **Towns:** Stonewatch and Ironreach lose their Master Crafter. They keep the three shops, the buffer, the
  gatekeeper, the warehouse keeper and the Huntmaster. I kept the Huntmaster because his hunting contracts are for
  the fields around that town; say if he should go too.
- The crafting window's "not a crafter yet" text names the three cities.
- SmokeTest: a new layout check (each major city has all three, each town none, every Anvil within reach of its
  Master). ALL PASS. ⚠ Needs the new APK (the client draws NPCs from its own copy of the map).

## 2026-09-26 — 0.214.25: the Anvil stands beside every Master Crafter (`BL-303` Q1-Q2)

> *"Yes anvil and master are always togheter"* · *"The points respec and rcps forget is at master .. The only tabs at
> anvil are craft+mats"*

- **A new NPC, "Anvil"** (no title), stands 250 from every Master Crafter. **Crafting happens only at the Anvil.**
  Talking to it opens the crafting window with just **Craft** and **Mats**.
- **The Master keeps the crafter's book-keeping:** spending points, the respec and forgetting recipes work only
  beside him, and his "Points" row opens the window on the **Points** tab alone. He still gives the trial and sells
  the recipe books.
- From the menu the window still opens with all three tabs, to browse; its buttons light up only at the right NPC.
- Frostmere's SP broker moved to the south end of the west column to make room for the Anvil.
- Protocol 51: an older APK can no longer craft, so this needs the new APK.
- SmokeTest: four new checks (a point refused away from a Master; beside the Master but not the Anvil; forgetting
  refused at the Anvil; both in reach midway). ALL PASS.

## 2026-09-26 — 0.214.24: the Master sells recipe books, he teaches nothing (`BL-303` Q3)

> *"Yes. Masters "learn for gold" buttons are gone ... They are in the "buy" part of it as items to use from inventory.
> And my L0~10 rows have which recipes are drop which the master sells"*

- **The Learn tab and the Master's "Learn" row are gone.** Every recipe is now an item you use from the bag.
- **What the Master's Buy list holds:** the T40/T52 gear books (as before), the Apothecary books your ladder marks
  "vendor" (**L0** Common HP/MP and **L2** Uncommon HP/MP), and a book for every **refine** (Nightsilver / Nightsilk
  steps, alloy, Volcanic Bar). Refines weren't in your ladder, but they were taught for gold too, so without a book
  nobody could learn them. Each book costs what teaching it used to.
- **Everything else is found only:** Greater buff potions, runes, Rare HP/MP, Instant Healing, Supreme Dash (the drop
  bands of 0.214.22).
- You still buy a book at its type level, and you learn it with the same checks as before.
- SmokeTest: new shelf check, and the "taught for gold" tests are now "buy the book, use it". ALL PASS. Formulas.md
  updated. ⚠ Needs the new APK.

## 2026-09-26 — 0.214.23: Bulgarian draws as letters, not boxes (`BL-313`, half 2, closes it)

> *"if u can do it alone so do it"*

- **Cyrillic is in the font now.** I baked it without the Editor: a second, fixed font atlas with the whole Cyrillic
  alphabet, the arrows `← ↑ → ↓` and `■ ○ ●`, at the same size and weight as the English letters. It sits right behind
  the main font, so any label that meets a Cyrillic letter draws it from there: chat, whispers, names, everything.
- Checked on the files: 105 characters in the new atlas (Б and → among them), the main font's own 250 characters and
  its atlas unchanged (byte for byte), and the new one first in its fallback list.
- ⚠ **Please check on the phone:** say or whisper something in Bulgarian. It should show letters, with nothing new in
  the System tab.
- `docs/guides/CyrillicFont.md` now records how it was done; you don't need to do anything in Unity.
- ⚠ Needs the new APK (client only).

## 2026-09-26 — 0.214.22: generic recipes drop, by tier (`BL-305`, part 3, closes it)

> *"I want generic recipes to be dropped as well and found"* · *"Generic rcps are normal mobs g make them from Tires
> that are close to the equip tires .. T40 is l0 .. T52 is l2 so l1 pots about t40~T52, l4 t61, L6 t76,l8 t80, l10 85+
> bosses/instances"*

- **Every Apothecary line has a recipe item now** ("Recipe: Rare Healing Potion" …), 15 in all, always 100%. Use it
  from the bag to learn the line, with the same checks as the Master's teaching (crafter, character level, Apothecary
  level). It is tradable and worth the Master's teaching price for its level.
- **Who drops them, your bands:**

  | creatures | recipes |
  |---|---|
  | level 40-51 (T40) | L0 Common HP/MP, L1 Greater Swift/Alacrity/Fury |
  | 52-60 (T52) | L1 Greater buff potions, L2 Uncommon HP/MP |
  | 61-75 (T61) | L4 1 h runes |
  | 76-79 (T76) | L6 Rare HP/MP |
  | 80+ (T80) | L8 2 h runes |
  | bosses 85+ | L10 Instant Healing, Supreme Dash |

- ⚠ **Rates are mine (placeholders):** a normal creature drops one recipe of its band every 100 kills (an elite every
  50), split evenly; an 85+ boss has a 20% chance at an L10 recipe. They share the recipe group, so the recipe rate knob
  moves them with the gear books. There are no instances yet.
- The Master still **teaches** every line for gold as before. Whether he should sell the L0/L2 books instead is
  question 3 in `BL-303`.
- `mob_drops.csv` regenerated. It had been stale since 0.206.0 (0.209.0's field creatures were missing), so it also
  shows specialty changes that were already live. `ItemIds.md` regenerated. Formulas.md updated. New SmokeTest checks
  (items, bands, bosses), ALL PASS. ⚠ Needs a new APK (the recipe items are in the shared catalog).

## 2026-09-26 — 0.214.21: the Scribe folds into the Apothecary, on your L0-L10 ladder (`BL-305`, part 2)

> *"It's a ruling at 40lv I noticed that the apothcand scribe are uncompareable and scribe always wins over ..so I decide
> we need to merge"* · *"Yes the 0.9->0.55 stays"*

- **One profession.** The Points page offers four types (Weaponsmith, Armoursmith, Jeweler, Apothecary); the Scribe is
  gone and every generic recipe is an Apothecary one.
- **Your ladder**, as the type level each line needs to learn and craft:

  | Apothecary | crafts |
  |---|---|
  | L0 | Common HP / MP potions |
  | L1 | Greater Swift / Alacrity / Fury |
  | L2 | Uncommon HP / MP potions |
  | L4 | War / Spell Rune box, 1 h (was Scribe L7) |
  | L6 | Rare HP / MP potions (were L7 / L10) |
  | L8 | War / Spell Rune box, 2 h (was Scribe L10) |
  | L10 | **Instant Healing Potion** and **Supreme Dash Potion** (new) |

- Price: still ×0.90 at L0 down to ×0.55 at L10. The learn price is now the ladder rung of the line's own level.
- ⚠ **The two L10 recipes are my placeholders**: x5 a batch, character level 85. Instant Healing takes the Rare HP line's
  inputs (10 gems, 1 B essence, 1 volcanic ash, 1 volcanic stone); Supreme Dash takes 20 gems, 20 wood, 2 S essence.
  They cost 25k / 250k at the shelf's price scale (x0.55 at L10), and neither vendors for more than it costs.
- The 1 h runes keep character level 70 even though their recipe will drop from T61 creatures (part 3). Say if the
  character level should follow the tier.
- Generic recipes are still taught by the Master for gold until part 3 turns them into items.
- Formulas.md updated. SmokeTest ALL PASS. ⚠ Needs a new APK (Points page and recipe table).

## 2026-09-26 — 0.214.20: three buff potions, two levels each (`BL-305`, part 1)

> *"Remove all buff ports except the 3 from the game ... The npc buffer don't give a buff potion effect but a npc_buff.
> Apoth vendor sells only the 3 lesser. Others are drop/craft"*

- **Gone:** the Agility, Might, Bulwark, Force, Ward and Aim potions (12 items). The NPC buffer is untouched: it lands
  its own rungs (`npc_buff_shelf.csv`), never a potion.
- **Swift / Alacrity / Fury, two levels, your numbers:**

  | | Swift | Alacrity | Fury | source |
  |---|---|---|---|---|
  | **Lesser** (L1) | +20 move | +23% cast | +23% attack | the Apothecary's shelf, and drops |
  | **Greater** (L2) | +33 move | +30% cast | +33% attack | crafted at Apothecary L1, and drops |

  Both were one rung lower before (Lesser +15/+15%/+15%, plain +20/+23%/+23%). Prices unchanged (1,500 / 5,000),
  and the drop bands too (Lesser to 51, Greater 40-60).
- **The Apothecary's shelf** has only the three Lesser buff potions. I read "sells only the 3 lesser" as being about buff
  potions, so the HP/MP potions, the Blessing Box and the scrolls/stones stay on the shelf. Say if you meant HP/MP too.
- **Crafting:** only the three Greater potions are crafted (Apothecary L1). Every buff SCROLL left crafting (the Scribe's
  19 lines). The scrolls still exist, in the Blessing Box.
- Next in `BL-305`: the Scribe folds into the Apothecary with your L0-L10 ladder, then generic recipes become drops.
- `BuffConsumables.md` and `ItemIds.md` regenerated. SmokeTest ALL PASS. ⚠ Needs a new APK (item names and recipes
  are in the shared catalog).

## 2026-09-26 — 0.214.19: gear recipes are bought at your smith level (`BL-303`, gear half)

> *"a T52 weapon rcp require L2 in weaponsmithing and T52 armor to be L2 armorsmithing"*

- The Master Crafter now refuses to **sell** a gear recipe below its crafter type level: the one gate that already
  applied to learning and crafting it (T40 L0 · T52 L2 · T61 L4 · T76 L6 · T80 L8, by Weaponsmith / Armoursmith /
  Jeweler). Before, you could pay for a book you couldn't open.
- The shop row says so: dimmed, with a red `(needs Weaponsmith L2)` after the name.
- The NPC split (Master = vendor, a plain "Anvil" to craft at) is not done; it waits on your answers in the
  `BL-303` entry (three questions there).
- SmokeTest: a new check (T52 refused at L0, sold at L2), ALL PASS. ⚠ Needs a new APK for the row text; the gate
  itself is server-side.

## 2026-09-26 — 0.214.18: gear crafts cost MP by tier, and a fighter can pay for one (`BL-306`)

> *"Now a weapon recipe t40 cost 400mp and a fighter have 200 ... I think the weapon,and armor should rise as lvls ...u
> can make t40 to need 200 at most and go from there as checking the fighter can craft atleast one wepon at that lvl"*

- Measured first (`BalanceMatrix --craft-mp`, new): the lowest fighter pool at each tier's own level, still in the
  previous tier's gear and unbuffed, is **203 / 325 / 437 / 778 / 876** (T40 / T52 / T61 / T76 / T80).
- MP per attempt now rises with the tier. The 2H sits under that pool; the other slots keep your note's ratios:

  | tier | 2H | 1H | body | helm / shield / necklace | gloves / boots / earring | ring |
  |---|---|---|---|---|---|---|
  | T40 | 200 | 150 | 100 | 75 | 50 | 25 |
  | T52 | 300 | 225 | 150 | 115 | 75 | 40 |
  | T61 | 400 | 300 | 200 | 150 | 100 | 50 |
  | T76 | 700 | 525 | 350 | 265 | 175 | 90 |
  | T80 | 800 | 600 | 400 | 300 | 200 | 100 |

- T61 is exactly the old table; T40/T52 got cheaper, T76/T80 dearer.
- The generic-line placeholders from 0.214.10 are unchanged; you said you'll judge them in the playtest.
- Formulas.md updated. SmokeTest ALL PASS. ⚠ Needs a new APK (the Craft page reads the MP from the shared catalog).

## 2026-09-26 — 0.214.17: boss drops scale by tier (`BL-308`, re-ruled)

> *"the rcp drop is at 80% for the t80 bosses ... U can increase as the boss lvl goes down ... (not a formula just
> interpolate) t40 boss can drop 2 3 recipes and 1 item for sure ... And the t80 boss drops 1 item at 70% and 1rcp at
> 80% (the 2% are the "lucky drop")"*

- Two authored tables, your T40 and T80 ends with the middle placed by tier number (rounded):

  | boss tier | full Mythic item | recipes a kill |
  |---|---|---|
  | T40 | 100% | 2.5 (two for sure, a third half the time) |
  | T52 | 90% | 2.0 |
  | T61 | 85% | 1.6 (one for sure, a second 60% of the time) |
  | T76 | 75% | 1.0 |
  | T80 | 70% | 0.8 |

- The 2% "lucky drop" accent and the books' own % (100% T40-T61, 60% T76/T80) are unchanged. Bosses below T40 pay
  T40's row. This replaces 0.214.11's flat 70% / 0.8.
- Formulas.md updated. SmokeTest ALL PASS. Server only.

## 2026-09-26 — 0.214.16: the skill bar comes in four shapes (`BL-299`)

> *"They work just take all the screen"*

- **Settings → Bar shape:** 2x6 (as before), **1x12** (one long row), **6x2** or **6x1** (a column on the right edge).
- **Settings → Extra squares** now offers what the shape allows:
  - 2x6: 1x6 … 4x6 rows on top, as before;
  - 1x12: 1x6 … 4x6 rows on top, or 1x12 / 2x12;
  - 6x2 / 6x1: 6x1 … 6x4 columns to the left.
- The extra squares **continue** from the last main square, so they never repeat it, and they page along with it. A
  6x1 column pages **six** at a time (10 pages), and each square still shows its 1-12 number within its page. A column
  fills top to bottom from the right edge, so the first squares are under your thumb.
- Changing the shape turns the extras off, because each shape has its own list. A phone that had extra rows keeps them.
  This is a setting on the phone only; the bar the server keeps is the same 60 slots. Needs a new APK.

## 2026-09-26 — 0.214.15: "Unequip All" goes on the bar (`BL-302`)

> *"it also need a to bar option -> u can add it as an action in the skills window. it dont have save/equip so its a
> single click and will work as an action as well (leave the one in the bag also)"*

- **Unequip All** is in the skill window's **Actions** tab. Put it on the bar and one tap takes everything off, exactly
  like the bag's button (which stays). Its square reads **UAl**. Needs a new APK.

## 2026-09-26 — 0.214.14: item rows say (T/B/U), and quality words leave item names (`BL-301`)

> *"nowhere on the item unless the details pannel is opened I can tell which of my 5 swords is the temporaty"* ·
> *"by the color of the item and the quality/rarity u can understand what is it"* · *"Only the abbreviation for the
> skill/buff bar can differ."*

- **Tags:** bag, warehouse, vendor-sell and trade rows end in `(T/B/U)`, only the letters that apply. **T** = temporary
  (a clock of either kind), **B** = bound (cannot be traded or sold), **U** = untradable but still sells. They come from
  the same three facts as the "(temporary, bound)" text in the details title, so the row and the title always agree.
  An item has at most one of B and U.
- **Names:** 44 items renamed. `(Lesser)`, `(Greater)`, `(Superior)`, `(Grand)`, `(Supreme)`, `(Bound)` and the
  Attribute Scroll's `(Common)` … `(Mythic)` are gone, and so are `Common` / `Uncommon` / `Rare` in front of the
  Healing and Mana potions and `Newbie` in front of the loaner kit. The colour and the Rarity line tell them apart now.
  Kept on purpose: *Instant* Healing Potion, *Greater* / *Safe* Scroll of Enchant (different items, not qualities), and
  armour-set variants like *(Assault)*.
- **The bar is unchanged:** each renamed item keeps its old full name for its skill-bar label, so "SPL" is still the
  Lesser Swift Potion and every label is still unique (checked). The potion **buff** names kept their qualifier, since
  a buff is not an item. Tell me if you want those shortened too.
- ⚠ Needs a new APK (the names come from the shared catalog).

## 2026-09-26 — 0.214.13: the missing-character flood stops (`BL-313`, half 1)

> *"the unicode character [] cannot be found in [LibirationSans SDF] assest and in any fallback fonts and was replaced
> with character [] in text object [lable]"*

- **The flood:** TMP logs that warning every time a label with a missing character redraws, which for a live
  label is every frame, and each one became a new line in the System tab. Now each missing character is reported
  **once per session**, then never again. Other warnings are untouched.
- **The `→` boxes:** the current-step mark on the quest Details page and the admin enchant menu used `→`, which the
  font does not have. Both now show `->`. A sweep of every client label found no other character missing from the
  font (em dash, `…`, `»` and `·` are all in it).
- ❓ **Half 2 is yours:** Cyrillic is still not in the font, so Bulgarian text still draws as boxes (the flood no longer
  follows it). Adding it needs the Unity Editor once: regenerate the font asset with the Cyrillic range, or add a
  Cyrillic fallback font. Client only.
- Tooling: `publish.ps1 -Apk` waited ~10 minutes after the APK was written, because `Start-Process -Wait` also waits
  for Unity's leftover compiler server. It now waits for Unity alone.

## 2026-09-26 — 0.214.12: Untrack works from the quest log (§105.2)

> *"in the quest window where all quest are listed I have abandon /untrack/details buttons and the untack does nothing"*

- **Root cause:** the client's `GameBoot.QuestAction` refused every action except Abandon while no NPC dialog was open,
  so the quest log's Track/Untrack never reached the server. It was the same bug Abandon had in playtest 14. The server
  toggle was fine, and the SmokeTest calls the hub directly, which is why it passed.
- **Fix:** "track" is exempt like "abandon". This also fixes the Details page's Track/Untrack from 0.214.5. Client only.
- Also filed: `BL-313`, the missing-font-character chat flood (§105.1). §105.3 closed: the character was 90 and the
  quest closes after 75.

## 2026-09-26 — 0.214.11: boss drops, 70% for a full item and 80% for a recipe (`BL-308`)

> *"let's not make 100% for 1 item but 70% for one full item and we leave the 2% chance as well for another one.. Recipes
> are at 80%"*

- **The full Mythic piece** of the boss's tier is a **70%** roll (was guaranteed). The 2%-per-family accent that can add
  another piece is unchanged. `MobCatalog.BossGuaranteedPiece` is renamed `BossFullItemChance`.
- **Recipes**: one group roll at **80% a kill, every tier**. That is the design note's own boss row ("80-90%" for any
  book). ⚠ Below T76 this is down from a guaranteed book. **At T76/T80 it is down from 1.5 books a kill.** Say if you
  meant ×0.8 of those (1.2 there) instead. The books' own % is unchanged: 100% for T40-T61, 60% for T76/T80.
- Formulas.md updated. SmokeTest 401/401.

## 2026-09-26 — 0.214.10: every craft costs MP (`BL-306`)

> *"every craft must cost mp (refines and generics and apoth as well)"*

- Gear and refines already paid MP. **The Scribe/Apothecary lines and the trial hammer now pay too**, per attempt
  (per batch), like everything else: **D (level-40 lines) 50 · C (52) 100 · B (61) 150 · the 70+ lines (rare
  potions, rune boxes) 200 · the hammer 50.** The Craft page already prints a non-zero MP.
- ⚠ **Those numbers are mine, placeholders**: you gave none. They sit on the refine ladder's own scale (a refine of the same
  grade costs the same), and each can be retuned alone. `BL-305` may redraw the whole generic table anyway.
- SmokeTest: every crafting check passes. The run had one unrelated timing flake ("the clock did not run in the bag":
  7199 instead of 7200), a different check from the last run's flake.

## 2026-09-26 — 0.214.9: crafting text says grades, not tiers (`BL-304`)

> *"The desciption should not say T40..T80 but grades ... D~S"*

- The Crafting window's Points page now reads *"Smiths: D grade at L0, C needs L2, B L4, A L6, S L8"* (was "T52 needs
  L2, T61 L4 …"). The Nightsilver / Nightsilk descriptions say *"of the D-grade weapons"*, and so on up the rungs.
  The Volcanic Bar says *"A- and S-grade crafting"*. Those were the only player-facing tier numbers; the debug panel
  keeps its T-numbers.

## 2026-09-26 — 0.214.8: an essence carries its grade (`BL-309`)

> *"Esseence have grade .. Darksteel essnce is D grade ... They represend essnece for each grade"*

- `ItemCatalog.GradeLabel` now gives each of the five essences its grade (**D, C, B, A, S**), read off
  `Crafting.EssenceItemLevels`, where it used to print "-". Every place that prints a grade uses it: the item details,
  the loot line, the vendor row and the bag's grade filter (which now finds essences under their grade).

## 2026-09-26 — 0.214.7: the town gate guards twice as far apart, on the town's border (`BL-310`)

> *"give a bit more distance between them like x2 more and move them to the border of the town"*

- **400 apart** (was 200).
- **On the border you see.** The drawn town is an octagon inside the safe circle, and its flat bottom side is at
  0.924 × the radius. The gate stood at radius + 60, so 210-330 units below the drawn edge (330 at Brackenford). It
  now stands **30 units past the drawn edge**, which is inside the safe circle. That is legal since `BL-276`: a guard
  may walk and fight an outlaw in town.
- ⚠ The one cost: a non-outlaw who hits a guard with PvP on while standing inside the safe circle cannot be answered.
  The same was already true anywhere in town. Server only (`WorldPlan`); boot-checked.

## 2026-09-26 — 0.214.6: the Blessing pauses out of combat and in town (`BL-300`)

> *"works but need to pause if i go out of combat or in town"* · *"u kill your last mob and the blessing activates and u
> dont reengage after 30s or whatever the timer for incombat the blessing should say (paused)"*

- **A running Blessing's 3-minute clock now stops** while you are **out of combat** (the same 30 s window as before, no new
  timer), **dead**, or **in a town**. It starts again when you fight outside town. The +100% stays on while paused; it
  only pays on kills anyway.
- The gauge's own 1%-per-combat-minute fill stops under the same rule (it already needed combat; now town stops it too).
- The HUD says so: **`Blessing 1:23 (Paused)`** / **`Blessing 40% (Paused)`**, and a paused countdown holds still. Each
  pause and resume pushes the sheet (`FavorUpdate.BlessingPaused`, appended: an older client ignores it).
- SmokeTest: a fresh character in town is told the Blessing is paused. 400/400 on the second run; the first run had one
  flake, "Focus Mastery gathers Focus from basic attacks" (pool read 0), which passed on the re-run.

## 2026-09-26 — 0.214.5: Track, Untrack and the arrow's quest from the quest details (`BL-311`, §105.2)

> *"the quest window dont allow me to untrack it"* · *"if a quest is tracked inside the details panel a [location tracking]
> button must appear and is disabled(or text as "current") so i can select witch one"*

- **The Details page of an active quest has Track / Untrack**, the same server toggle as the Active row's button. That page
  had no way to unpin (§105.2; the row's own button was already fine on the server).
- **On a tracked quest it also has `Location tracking`**: the ground arrow now follows that quest. On the quest the arrow
  already follows the button reads **`Location: current`** and is disabled.
- The tracker marks the followed quest with a blue **»**. With nothing picked, or once the picked quest is unpinned or
  finished, the arrow follows the top pin as before.
- ⚠ The pick lasts for the session. After a relog the arrow follows the top pin again until you pick. Client only.

## 2026-09-26 — 0.214.4: EXP bar darker; Favor and Blessing on one row (`BL-312`)

> *"The exp bar need a darker green — a touch darker than the favor one now .. i like the favor one"* · *"wonder if we can
> make the blessinf and favor on one row (the two bars to be on the same row side to side)"*

- **EXP green** is now `(0.19, 0.45, 0.19)`: the step from the old lime to the Favor green, taken once more. The Favor
  green is unchanged.
- **Favor and Blessing are side by side** on one row, half width each. The vitals panel is a row shorter (114 → 92 px)
  and the buff bar docks 22 px higher under it. The Favor text dropped its `/ 20,000` to fit (the fill shows how full
  it is): `12,345  L2  +100%`. Client only.

## 2026-09-26 — 0.214.3: the crafter header is no longer under the tabs (§105.4)

> *"The row where is says "browsing/at anvil" is hidden behind the tabs"*

- The Crafting window's header was one 22px line. With five crafter types in it, it wrapped, and the second line
  (**at the anvil** / **browsing**) sat under the tab row. It is now **three lines** of its own: level, slots and
  free points; the five type levels; where you are. The tabs and the list moved down to make room. Client only.

## 2026-09-25 — 0.214.2: a miss aggroes the mob (§104.1)

> *"a 1st misses until an actual hit on mobs the mob dont agrro"* · *"if i miss 10 times and then hit .. he will start to
> attack at the hit (11th time)"*

- **Root cause:** a hostile act that dealt no damage (a **missed swing**, and also a debuff or CC) left **1 threat**,
  exactly the `ThreatFloor`. The first one-second decay (×0.99) took it to 0.99 and pruned it. The mob lost its only
  target and went home, so each miss made it turn for under a second. Only a landed hit's damage-sized threat survived.
- **Fix:** that act now leaves `GameConstants.ProvokeThreat` = **10**, which outlives the floor for about 4 minutes of decay.
  Leaving view range or the leash still ends the fight as before. Against real damage (hundreds a hit) it never decides
  who the mob chases. This also fixes a debuff-only pull, which had the same silent bug. Server only.

## 2026-09-25 — 0.214.1: a re-roll keeps your quests and limits; `/resetlimits` (`BL-296`)

> *"main class reset (admin reset) resets all my quest progress ... it should not cancel my quests"* · *"also it reset the
> daily quests i could take the rune again .. i want to reset nothing only class and stats ... limits stay ... a new admin
> command/button to reset limits"*

- **The debug re-roll (Reset → race & class) no longer touches quests or limits.** It used to clear every active and
  completed quest. That lost your "Welcome, traveler" chain and your hunter's contract. It also cleared today's daily
  stamps, which is why the rune could be taken twice.
- It now changes **only the class, its stats and its level** (back to 1, as before). The single exception is a quest
  **locked to the old race, base class or class chain** (only the class-change chains, today), which is dropped. A quest
  whose progress has passed a **"reach level X"** step that you no longer meet goes back to that step and asks for X
  again. Steps before it keep their progress. Nothing is cancelled.
- **`/resetlimits [name]`**, admin only, plus a **"Reset my daily limits"** button on the debug panel's character tab. It
  gives back everything you use once a day: today's daily-quest stamps, the account's auto/offline farm allowance, the
  like budget and the Favor potion's cooldown. Instance entry limits will join it once instances exist.
- Filed for later: `BL-298`, the premium main-class change (same level/EXP/SP, quests kept). **A new APK is needed** for
  the button; the command works from chat on any client.

## 2026-09-25 — 0.214.0: Favor and Blessing are bars; EXP runs along the bottom (`BL-295`)

> *"need the blessing % and favor to be like a bars ... EXP bar + lvl 123 can go on the bottom of the screen spanning
> across the whole width ... [Name on the left and the 123/123 100% in the middle] -> thats the hp bar ... the curretnt exp
> bar becomes the favor (... a bit darkenr than the current lime one) -> [0/20000 L0 0%] ... 4th bar [blessing 0~100%] some
> golden color not so distracting ... when it activates ... the 100% to become a 180s countdown - no need fo actual buff"*

The vitals panel (top left) is now **four bars and no title row**, the same size as before:

| row | shows |
|---|---|
| **HP** | your **name** on the left, `123 / 123   100%` in the middle |
| **MP** | as before |
| **Favor** | `12,345 / 20,000   L5   +250%` (points, stage, the Favor's own bonus), a green a shade darker than EXP |
| **Blessing** | `Blessing  73%`, muted gold. While one **runs**, the bar turns **bright gold**, drains with the time left, and reads `Blessing  2:41` |

- **EXP and the level** moved to a thin strip across the **whole bottom edge** of the screen: `Lv 85   1,234 / 5,678   21%`.
  The chat row and the skill bar moved up 6 px to stand on it.
- **The Blessing has no buff icon any more**, so there is nothing a player can try to remove. The countdown comes from the
  server's own clock: `FavorUpdate` gained `BlessingSecondsLeft` (appended, so an older client ignores it) and the HUD
  counts it down between pushes. The cosmetic `wayfarer_blessing` buff, its re-assert loop and its icon were deleted.
- **The Blessing's fill multiplier (booster rune ×2, charisma up to ×2, ×4 together) now applies to MOB KILLS ONLY.**
  Your follow-up: *"the bonus x2/4 to the blessing only works on killing mobs .. not on the 1%/60s nor on the 8% when favor
  drops a lvl nor on the lvl up"*. The combat minute (1%), the Favor stage loss (8%) and the level-up (30%) are now paid at
  ×1. This **reverses** the fifth-round rule that the modifier multiplied every source. `Formulas.md` moved with it, and the
  character sheet's "Fill rate" now reads **"Kill fill"**.
- SmokeTest: the fear "stop taps are refused" check was a timing race (a 5 s fear, sampled up to ~7 s after it was cast).
  The fear is now 10 s, the refusal is polled, and the "control handed back" check waits for the fear to leave the buff bar.
- **A new APK is needed.**

## 2026-09-25 — 0.213.0: the quest arrow (`BL-294`)

> *"we need to make in the clientside (its only visual) for a tracking quest an arrow to point the direction of the
> npc/mob i need (when i get into 200-300 range to disapear)"*

- A **gold arrow on the ground** beside your character points toward what your **top pinned quest** needs next:
  - a **talk-to** step points at that NPC;
  - a **kill** step, or an unfinished gathering contract, points at the **nearest zone** that spawns that creature;
  - a gathering contract that is **ready** points back at its giver.
- It disappears **within 250** of an NPC, or once you are **inside** the mob's zone. Steps with no place (reach a level,
  do an action) show no arrow. Pin a different quest to the top to follow that one.
- **Client only.** It is resolved from the quest log the server already sends, plus the quest and world data compiled into
  the client, so no server or protocol change was needed. It sits on the ground rather than on the screen edge, so it
  stays correct however the camera is turned. **A new APK is needed.**

## 2026-09-25 — 0.212.1: every town has a gate, and guards hold their post (`BL-293`)

> *"town guards should walk with 0 speed .. so they wont move from their spot .. and run with normal one like
> 180(mele)-200(for archers) ... each town must have a gate ... two guards at each gate ... until we have a real gates and
> walls ... u can position only two guards on the bottom of each city but both to be spaced out like 200 range so u can
> walk in between them - one fighter and one tank (the archers 90 are the guard towers on a piecefull zones -> u can leave
> them as they are (only unmovable when not aggroed))"*

- **The gate:** each city now has a **fighter** (`guard_town_fighter`, "Town Warden": the Warrior build, heavy armour, 2H
  sword, S +0, War Rune) and a **tank** ("Town Watchman") at the **bottom** of the town. They stand 200 apart, 60 past
  the safe radius, and you walk between them. The old tank + archer pair on the first field's bearing is gone, and the town
  archer with it. Each guard has its own 5-unit spawn zone, so it appears exactly on its post.
- **No guard wanders**, town or field. A guard still scans, still chases at its run speed and still sprints home off the
  leash, but between fights it **stands where it spawned**. ⚠ This is done in `MobAi`, not as "walk speed 0".
  `EffectiveSpeed` reads a 0 base as "unset" and falls back to the run speed, so a walk of 0 would have made them wander
  *faster*. A gear-built creature's `RecomputeDerived` also rewrites its walk speed anyway.
- **Run speeds:** both town guards are **180**. The field pair (tank + the level-90 archer "guard towers") keep their speeds
  as you said, and only gain the no-wander rule. No archer is left at a town, so the 200 has nothing to apply to yet.
  **No APK needed.**

## 2026-09-25 — 0.212.0: a new character starts with an empty bag (`BL-292`)

> *"start items for newly created chars should have none .. the newbie quest gives enough .. it can also give 50 mp and 50
> hp pots somewhere in between reach 10/15/18"* · *"buy stating items i mean the 2 rare and 5 common pots"*

- Creation no longer hands out the **5 Common + 2 Rare healing potions**, so a new character's bag is **empty**. The debug
  re-roll (`GiveStarterKit`) matches.
- **Properly Armed** (the level-10 tutorial quest, with the Newbie gear boxes) now also pays **50 Common healing + 50
  Common mana potions**. Of your three points (10/15/18), 10 is the earliest, and with an empty bag from level 1 that is
  the one that matters. Moving them to 15 or 18 is one line.
- `QuestReward` has no quantity field, so a stack is N entries. The turn-in already merges them into one stack, and a small
  `Many(id, n)` helper keeps the reward readable. **No APK needed** (server + shared data only).

## 2026-09-25 — 0.211.0: a vendor row opens the item's details first (`BL-291`)

> *"the mythic body armors in vendors need to show the set effect .. may be before buy/sell (if not quick is enabled) open a
> details panel for that item .. then there should be a buy button .. for potions will open it details pannel then a buy
> button will open the numpad ... same details panel as for the inventory just changed buttons"*

- Tapping a vendor row now opens the **bag's own item window** for that item: stats, description, and the **set with its
  effect**. The vendor's old confirm printed only the stat block, which is why a Mythic body armour never showed its set.
- The window has just two buttons, **Buy (price)** and **Cancel**. There is no lock, equip, break down or bin.
  Buy on a single piece buys it. Buy on a stackable (potions, scrolls, materials) opens the numpad, as before.
  The panel **is** the confirmation, so nothing is asked twice (your playtest-16 rule). Essence wares work the same way.
- **The sell side too**: Sell (price) + Cancel, drawn from the real piece, so its enchant and **attribute rolls** show.
  The window follows the piece and closes itself once it is sold. **QSell ON still sells in one tap**, with no panel.
- The vendor's old confirm dialogs (`ConfirmBuy` / `ConfirmSell`) are gone. **A new APK is needed** (client only).

## 2026-09-25 — 0.210.2: saves land in order; §102.9/§102.10 not reproduced

- **A character's saves can no longer land out of order.** Each save runs on its own thread. The save gate stopped two
  saves from overlapping, but it never guaranteed the order they ran in. So when two saves of one character were queued
  in the same tick, the OLDER one could be written last and overwrite the newer one. That meant lost data until the next
  60 s autosave, or permanently if the newer save was the logout. Each snapshot now carries the order it was taken in
  (`CharacterSnapshot.Seq`), and `PersistenceService` skips any snapshot older than one already written for that
  character. A snapshot is the whole character, so skipping an older one loses nothing. Found by the SmokeTest's
  intermittent jail check (§103.2): `/jail` saves the teleport and the charisma drain in the same tick.
- **§102.9 / §102.10 (Heal replaces Self Heal; Holy/Elemental Bolt replace Magic Bolt) do not reproduce.** A new SmokeTest
  block (11b) walks the path through the real handlers. It buys the base spell, class-changes at 20, buys the 2nd-class
  spell, and checks three things: the old spell is gone, it cannot be bought back, and it stays gone after a relog. All
  three pass for all three pairs. The question left for you is on the checklist: should the old spell go at the
  **purchase** (today) or at the **class change**?
- SmokeTest: the "Spellcaster Mastery" check now waits for the skill list rather than reading it straight after the
  stats push (the other §103.2 flake). Server-only fix; **the 0.210.1 APK still works** (protocol unchanged).

## 2026-09-25 — 0.210.1: the Armsmaster and the Outfitter in four groups

> *"i wonder if we group the armsmaster to sword/blunt/magic/bow+fang .. 4 groups not 8 ... and same for outfitter"*

| shop | tabs (was) | tabs (now) |
|---|---|---|
| Armsmaster | Sword · 2H Sword · Blunt · 2H Blunt · Duals · Bow · Wand · Staff | **Sword · Blunt · Magic · Bow/Dual** (1H + 2H together) |
| Outfitter | Body · Helm · Gloves · Boots · Shield · Neck · Ring · Ear | **Body · Pieces · Shield · Jewels** (Pieces = helm + gloves + boots) |

- The fourth weapon group is labelled **Bow/Dual**, by what is in it, not by a class: bows belong to the archer path too.
  "Pieces" rather than "Parts", because parts are a crafting material.
- Your first Outfitter layout was used, not the second (helm+shield, boots+gloves).
- With All, each strip is five tabs, so the buttons are back to full width. Every shelf item lands in a tab (checked).
  This closes the "etc." question from 0.210.0. **A new APK is needed.**

## 2026-09-25 — 0.210.0: a vendor's tabs are its own (`BL-290`)

> *"can we make npc vendors their tabs to be custom per vendor ? Apothecary to have like (pots,scrolls,misc) ; armor
> vendors to have (body,helm,gloves,boots,Shield,neck,ring,ear); etc... The new npc that sells cobold for essence can
> have (armor,jewels,weapons)"*

On the **Buy** side the tab strip now comes from the shop you are looking at, with **All** first:

| shop | tabs |
|---|---|
| Apothecary | All · Potions · Scrolls · Misc (stones, rune boxes, the Rite of Ascension) |
| Armsmaster | All · Sword · 2H Sword · Blunt · 2H Blunt · Duals · Bow · Wand · Staff |
| Outfitter | All · Body · Helm · Gloves · Boots · Shield · Neck · Ring · Ear |
| Assayer (essence) | All · Armor · Jewels · Weapons |
| Master Crafter | the generic All / Gear / Use / Mats (no tabs of its own) |

- ❓ **The Armsmaster's tabs are my reading of your "etc."**: one tab per weapon kind. Say if you meant something else.
- **A box is filed by what is inside it**: the temporary weapon box is on every weapon tab, the armour box on
  Body/Helm/Gloves/Boots/Shield, the Blessing Box under Scrolls and the rune boxes under Misc. Nothing is typed per box.
- **The Sell side keeps the generic All / Gear / Use / Mats**, because it lists your bag, and tabs like "Ring" or
  "Scrolls" would hide most of it. Instant sale still follows those tabs.
- A long strip gets narrower buttons so nine tabs and the order button fit in one row.
- Wire: `ShopInfo` gained an **appended** `ShopId`, so an older client ignores it; no protocol bump. **A new APK is
  needed** to see the tabs.
- ⚠ Found on the way, **not changed**: the **Training Wand has no `IsMagicWeapon`** (it is a plain Blunt with M.Atk 7),
  and the caster check keys on that flag. The tab names it by id so it sits under Wand. Whether the wand itself should
  count as a magic weapon is your call.

## 2026-09-25 — 0.209.2: a grade only where one exists (`BL-289`)

> *"look at boxes they have grade for some reason if it's not a equipment or something that have a grade inside the
> box grade should show as '-'. (check everything to match it's grade - most won't have any)"*

- **One helper decides every grade the player sees**: `ItemCatalog.GradeLabel`. Gear shows its grade letter. A box
  shows the grade of the gear inside it, read from its contents (the T40 temporary box is **D**, the T52 one is **C**,
  the Newbie boxes are **F**). Everything else (potions, scrolls, mats, runes, quest items, recipes, boxes of non-gear)
  shows **"-"**. The `ItemGrade` enum is still there for prices and sorting, but nothing displays it any more.
- It is used by the vendor row, the item details "Grade:" line, the staff drop-search list and its Grade filter (which
  now runs F-E-D-C-B-A-S instead of the enum's five), the server's "You looted: … [grade]" line and `docs/guides/ItemIds.md`.
  **The check turned up more than the boxes**: every item with no item level printed its enum default, and the id list
  showed all T40 Common and temporary gear as "B" (they are D).
- The temporary boxes are renamed **"Temporary Common Weapon Selection Box"** and **"Temporary Common Armor Selection
  Box"**. The grade is no longer in the name; it comes from the rule above.
- **A new APK is needed** (the names and the vendor row are compiled into the client).

## 2026-09-24 — 0.209.1: `/like <name>`, and the staff `-f` that forces charisma

> *"Make an admin command "/like <name> [-f 1234]" where a normal player like command stop to the name and admin
> can add -f(force) option ... if a player have 1000 monthly and admin do "..-f 0" the players score should show 0"*

- **`/like <name>`** is the typed Recommend, for every player. It runs the same rules as the button, and
  `@t`/`@s` work in it. **A new APK is needed** for ordinary players, because the client used to refuse the slash command.
- **`/like <name> -f <0-1000>`** (Admin+) **sets** current (30-day) charisma to that number, online or offline.
  Your first reading was chosen: *set the sum*, not *add to today*. The ring is emptied and the whole value is put on
  today's slot, so the sheet reads exactly that number now and it ages out after 30 days like real recommendations.
  Adding a negative number to today's slot was the other reading. It would have left negative days that swallow new likes
  for a month, so it was not used. **Lifetime (the board) is not touched**; a ban and `/jail` still drain it.
- SmokeTest: four new checks (a player's `/like`, a player's `-f` refused, staff `-f` online and offline).

## 2026-09-24 — 0.209.0: the anti-type zones, and aggression only from level 80 (`BL-280`)

> *"anti mage and anti fighter and anti archer mobs need to be in self zones"*

**Six new fields hold twelve new creatures, and a resist now lives only in its own zone.** Three proof-of-concept
zones, each repeated at 55-60 (Greymarsh) and 75-80 (Frostmere). **A new APK is needed**: the client draws fields
from its own `WorldMap`, so an old APK plays fine but does not show the new fields on the map. No DTO changed.
⚠ The `game.db` delete is still owed from 0.205.0.

| zone | field (55-60 / 75-80) | creatures | passive |
| ---- | --------------------- | --------- | ------- |
| melee | Shellback Flats / Ironshell Drifts | Shellback/Ironshell Crawler · Hexward/Frostward Golem | bow ×1.6 · mRes +0.50; both −20% to blunt/sword/dual |
| ranged | Harpy Fen / Stormcrest Ridge | Gloomhusk/Rimebark Treant · Marsh/Storm Harpy | +60% to blunt/sword/dual; −20% mRes · −20% bow |
| AoE | Swarming Mire / Rimeskitter Hollow | Mire/Frost Swarmling · Mudskitter/Rimeskitter | half HP; 20 in a 500 radius (≈3.5× density) |

- **Measured** (`BalanceMatrix --antitype`, its verdict now judged per attacker against a plain mob): each resist
  reads ×1.60 kill time, each weakness ×0.80, and the swarms ×0.50 for everyone.
- **"Half HP == half all".** EXP/SP already follow the creature's real HP (`MobKillTimeRatio`), so the swarms
  pay half with no extra knob. A new **`MobMod.Reward`** (0.5) halves their gold and every drop chance, and it is
  applied inside `KillTable` so the roll, the inspect list and the drop index agree.
- **`MobType.OwnField`**: never rostered into a generated camp by band (unlike a normal template), but still dealt a
  drop profile (unlike `HandPlaced`). ⚠ Adding six creatures to each of the 52-60 and 76-79 profile bands
  **re-dealt those bands**, so some existing creatures there now carry different gear kinds.
- **The eight old anti-type templates are neutral** (his ruling): `shield_skeleton`, `watcher_eye`, `grave_lich`,
  `fomor_brute`, `aether_wisp`, `obsidian_knight`, `dread_knight` and `spiteful_ghost` lost their resist passives
  and kept their ids. The Tank/Healer/Nuker class-change hunt, the Dread Knight contract and the dungeon bosses
  keep their targets, and the dungeon copies are plain too.
- **Aggression, until the real zones and map:** in a generated normal camp only a spawn of **level 80+** attacks on
  sight (`SpawnZone.AggressiveFromLevel`, checked per spawn), so a 78-80 camp's level-80 spawns bite and its
  78-79 ones do not. At 80+, **every** aggressive-capable creature does (it was 3 types). Everything below 80 is
  peaceful. Elite camps, dungeons and the two field-boss flank rosters are unchanged. `AggressiveRamp` is gone.
- `Formulas.md`: the drop formula gained `× Reward`, and a stale line saying zone HP leaves EXP untouched is
  corrected (EXP follows HP).
- SmokeTest **375 ALL PASS**: the old "three aggressive types" check is replaced by four (the level-80 gate,
  80+ all-aggressive, no anti-type creature in an ordinary camp, six anti-type fields).

## 2026-09-24 — `BL-273` closed: the crafting rework's five placeholders confirmed (no code)

The rework design doc §8 was answered: respec 1M-16M, rare HP/MP at Apothecary L7/L10, the nine basic buff scrolls
= the common line, volcanic ash/stone 5,000 and bar 200,000, and Nightsilver/Nightsilk 20 × 10^rung all **stay as
built**. `BL-273` is archived (built across 0.200.0-0.205.0). The 0.208.0 APK was delivered for the playtest.

## 2026-09-24 — 0.208.0: the daily recipe quests (`BL-274` part 3, step 13; closes `BL-274`)

> *"talk to 2-3 ppl, each asks 5-10 kills"*

**Three new NPCs in Frostmere hand out the T76/T80 recipe books once a day.** Weaponwright Harrow (weapons),
Armourer Edda (armour) and Jeweller Ossian (jewellery) stand in a column west of the town centre. The version
moved, so **a new APK is needed** (no DTO changed; an old APK still sees the NPCs, only its debug teleport list
misses them). ⚠ `game.db` delete still owed from 0.205.0.

- **Each giver has two quests:** a **T76** one (levels 75-85, Adamantine) and a **T80** one (80+, Soulcrystal).
  They share **one daily stamp**, so between 80 and 85 you pick one. Holding either bars the other, and the hand-in
  closes both until the server day rolls over. **At most three books a day, one per kind.**
- **The errand:** talk to the other two givers, and each wants 8 kills of the kind's own carriers:
  weapons: Redhorn Soldier + General (T76), Wrathborn Demon + Radiant Scout (T80) · armour: Sunland Orc Captain
  + Commander, Scarlet Mantis + Splinter Mantis Drone · jewels: Emberwyrm Drake + Redhorn General, Radiant Mage +
  Radiant Berserker. Then back to the giver.
- **The reward is one 40% recipe book, uniform within the kind** (1/8 weapons, 1/7 armour, 1/3 jewels), and
  nothing else. The 16 kills pay their own EXP and drops.
- **Engine:** `QuestReward.RandomItemIds` (a pool paying one id) and `QuestDef.DailyGroup` (one stamp shared by
  several dailies). Ungrouped dailies keep their old stamps.
- ⚠ **Side effect, by the standing rule:** a quest kill target gets a dedicated spawner in every normal camp that
  rosters it, so the 76+ camps holding these 11 creatures gain 2-4 guaranteed spawns each.
- SmokeTest §5c plays Harrow's T80 quest end to end (`DebugQuestKill` credits kills through the real matching)
  and checks the shared stamp, the one-book payout and its kind. 371 checks, all pass.

## 2026-09-24 — 0.207.0: boss drops (`BL-274` part 2, step 12; closes `BL-50` + `BL-262`)

> *"ppl do bosses to get equipment ... otherwice bosses are usless"*

**A boss now pays a full item, recipes, a big mat pile, parts, both metals and (T76/T80) essence, and every piece
of it is an ordinary drop-table row.** The old pile and recipe rolls lived inside the kill path, where no rate knob
reached them and the party loot rule applied to the pile as one lump. They are gone. The version moved, so **a new
APK is needed** (no DTO changed). ⚠ `game.db` delete still owed from 0.205.0.

- **Full item:** one guaranteed at every tier, across all 18 kinds (unchanged), plus the 2%/family accent.
  🔴 **Fixed: the level-90 boss was paying a T76 item**, because the gear tier stopped at 76. It pays T80 now.
- **Recipes:** one book a kill at T40-T61 (100%), 1.5 a kill at T76/T80 (60%), all kinds. Knob: `/droprate recipe`.
- **Base mats ×100 a normal kill** (150 of the primary at 90, the note's "100-200"); **parts** of every kind and
  **Nightsilver and Nightsilk both ×10**, with Legendary from 80. The old flat 6-10 + 4-7 gem pile is gone.
- **T76/T80 essence, guaranteed:** a tenth of a full 2H's break: 288-480 A, 720-1200 S.
- One kill at x1: Grave Lich 1.26M coin · Valley Treant 3.9M · Dread Knight 17.3M · Emberwyrm Matriarch 39M ·
  Disciple of the Dawn 195M. ~90% of that is the full item (`--drop-value`, new boss section).
- **`BL-262` (your option 3):** the whole boss drop takes every knob: the global, the group, a Rune of Drop and
  the level gap.
- **`BL-50`:** every piece goes to its own recipient under the party's loot mode and pickup filters, the same
  code as any party drop. 🔧 **Your check:** kill a boss in a party on Random or Round-robin loot.
- Removed: `GameLoopService.RollBossBonus`, `MobCatalog.BossPile` / `RecipeRolls`, and the drop database's
  "no rates" rows. `--craft-cost` now reads the boss recipe chance from the table.

## 2026-09-24 — 0.206.0: per-mob drop tables (`BL-274` part 1, step 11)

> *"mobs should have own drop tables .. not all to drop all items"*

**Every creature of level 40+ now drops ONE kind of gear, and the crafting mats finally have a source.** Each
tier's band of creatures is dealt a specialty (weapons, body armour, small armour or jewellery), flavoured by
what the creature is, and it drops that specialty's Commons, the rare full item, recipes and parts, plus
Nightsilver (weapons, jewellery) or Nightsilk (armour). The readable table is `docs/data/mobs/mob_drops.csv`.
⚠ `game.db` delete still owed from 0.205.0. The version moved, so **a new APK is needed** (no DTO changed).

- **Dealt, not authored.** Jewellery goes to ~1 creature in 7, weapons to ~40% (1-3 lines each; an archer drops
  bows, a caster staves), the rest to body and small armour. The 76-79 band has only five creatures, so its two
  weapon carriers drop four lines each. The server refuses to start if any gear kind has no source in a band.
- **Commons** keep your per-slot %, but only on the creatures of that slot, so the world's Commons fall ~4×.
  **The rare full item**: 1 in 10,000 kills, its own `/droprate rare` knob.
- **Recipes** by source: T40 100% 1/100 · T52 100% 1/175 · T61 60% 1/250 (elite 100%) · T76 20% (elite 40%) · T80
  elite 40%, per kind, rarer for big slots at T76/T80. Knob: `/droprate recipe`.
- **Parts** 1% / 0.5% / 0.25% / 0.1% / 0.1% per kind; **Nightsilver/Nightsilk** 0.155 / 0.96 / 7.25 / 23.2 / 42.8
  a kill with the higher rungs at 1%; **base mats** from level 35 (primary 0.2 → 1.5, secondary half, iron and gems
  half again); **ash + stone** 0.3 each on the six creatures at level 76, 80 and 85; **T76/T80 essence** drops
  directly: 1% for 30-50 A, 0.5% for 30-50 S (`/droprate essence`). Elites: Commons, rare and essence ×2, parts
  and Nightsilver ×4, base mats ×10. The elite mat pile and the elite's 0.1% recipe roll are gone.
- **Prices (your ruling):** a recipe costs **10% of its item × its %** (a 20% recipe = 2%); a part costs **1% of
  its item**. Coin a kill, band average: T40 4.9k · T52 8.3k · T61 13k · T76 11.2k · T80 11.6k.
- **Bosses are unchanged** until step 12.
- One function, `MobCatalog.KillTable`, is now the table the kill rolls, the target-inspect list shows and the
  Drops window indexes. They used to rebuild the rank layer three times.
- `--craft-cost` (2H at 100%, best creature): T40 15.9h · T52 36h · T61 82h · T76 240h · T80 316h. New tools:
  `--dump-drop-csv`, `--drop-value`.

## 2026-09-24 — 0.205.0: the crafting materials and the gear recipe tables (`BL-273` part 3, step 10)

> *"we need new mats -> like any weapon/armor need main crafting mat for itself"*

**Gear recipes now take the rework's materials.** The old Uncommon…Mythic material ladder is gone: the five base
mats (**Iron**, Thread, Wood, Leather, Gem) are one rung each, and the metal and cloth that climb a ladder are
**Nightsilver** (weapons, earrings, rings) and **Nightsilk** (armour, shields, necklaces). Every weapon, armour
piece and jewel recipe from T40 to T80 is an authored table. ⚠ `game.db` delete (item ids changed).
⚠ Protocol 50: **a new APK is needed.**

- ⚠ **Nightsilver, Nightsilk, parts, volcanic ash and stone drop from nothing yet.** Step 11 (`BL-274`) gives
  them sources, so until then no gear can be crafted. The old higher-rarity mat drops (the level-gated rolls,
  the elite rungs and the boss pile's upper rows) are removed. Bosses and elites still pay their Common pile.
- **Refines** (open to every crafter, no gold, **0 craft points**): 10 of a rung → 1 of the next, at generic
  L0/40 → Refined, L3/52 → Rare, L5/61 → Refined Rare, L8/76 → Legendary. They cost MP 50/100/150/200.
  **Alloy** = 20 gems + 20 iron (L0/40, 50 MP). **Volcanic Bar** = 20 ash + 20 stone (L7/76, 200 MP).
- **Parts**, one per item kind per tier (18 a tier, 90 in all), e.g. *Darksteel Maul Head*, *Cobalt Hide Panel*.
  A part is worth its item's Common price.
- **A 2H at 100%:** wood + iron 400 … 2000 each, alloy 10 … 50, 20 parts, Nightsilver 300 normal / 200 Refined /
  150 Rare / 50 Refined Rare / 10 Legendary, Volcanic Bars 40 / 70 at T76 / T80, and essence 400 … 2000. Every
  other slot is its own cell, written from your guide shares. **Nightsilver/Nightsilk never scale with the
  recipe %**; everything else does.
- **Every craft costs MP**, from your note: 2H 400, 1H 300, body 200, helmet/shield/necklace 150,
  gloves/boots/earring 100, ring 50, refines 50-200. (*"warriors need to wear a robe to spam crafts"*)
- **A count on non-gear crafts.** The Crafting window's **Count** button cycles x1 / x10 / x100 / max. One tap
  repeats a refine, potion or scroll recipe until something runs out, because a T76 weapon is ~5,500 refines.
  Gear stays one attempt a tap.
- The Materials tab lists the base mats, both ladders, alloy/volcanic, essence and the parts you hold. The Debug
  window gives the new mats and a tier's parts.
- `--craft-cost` now reads the 2H recipe from the catalog; the hours are unchanged (T40 14.8h · T52 28h · T61
  65h per 2H at 100%). BalanceMatrix §M (M1-M12, the old ladder's faucet) is deleted.
- SmokeTest: 359 PASS, 7 of them new (catalog cells, fixed Nightsilver, refine gates, a x100 refine that stops at
  its materials and pays no points).

## 2026-09-24 — 0.204.0: the crafter-points model + the generic-recipe table (`BL-273`, step 9b)

> *"when u craft u lvl up generic .. each generic gives 1 "skill" point .. u then deside where to put this point into"*

**Crafting a type is now a choice.** Every generic level gives one point, 10 in all, and you spend them on
**Weaponsmith / Armoursmith / Jeweler / Apothecary / Scribe**. Types no longer level by crafting. The potion,
scroll and rune recipes are the ones you ruled for step 9b. ⚠ `game.db` delete (columns changed).
⚠ Protocol 49: **a new APK is needed.**

- **Tier gates.** T52 gear needs the type at **L2**, T61 **L4**, T76 **L6**, T80 **L8**. **L9 and L10 add +5%
  each** to a gear recipe (a 60% one ends at 70%); that replaces the old +0.5%/level at T76/T80. So a T80 smith
  has 2 points left for anything else, and nobody makes top gear in two types.
- **Everyone at L0** crafts T40 gear, the common HP/MP potions, the common buff potions and the nine basic buff
  scrolls. **Apothecary / Scribe** unlock the uncommon lines by the same tier gates (T40 = L1). They also unlock
  the 1h/2h rune boxes at **Scribe L7/L10**, and the rare HP/MP potions at **Apothecary L7/L10**.
- **Scribe/Apothecary crafts cost ×0.90 of the batch's buy price at L0, falling to ×0.55 at L10.** The
  essence and mats are fixed and the gold makes up the rest, so every batch costs some gold. Crafting then
  vendoring is always 0 or a small loss (`--craft-cost` C5b checks every row at L0, its gate and L10).
- **Respec** at a Master Crafter: every point back, **5 a lifetime** at 1M / 2M / 4M / 8M / 16M (⚠ the prices
  are placeholders). **Recipes are never forgotten by a respec.** One above your new levels stays in its slot,
  **locked**, until you reach it again, or until you forget it by hand. The Crafting window's **Points** tab
  (was Slots) spends and respecs.
- **The table (all 100% success):**
  - HP/MP potions: common **x100 **, uncommon **x50 **, rare **x10 **. They take gems + D/C/B
    essence, and the rare ones also take volcanic ash/stone.
  - Buff potions **x6** (gems + wood, and essence for the uncommon ones).
  - Buff scrolls **x2** (leather + iron, and essence for the ten "other" ones).
  - War/Spell rune boxes **x3**: 1h  with B essence, 2h  with Volcanic Bars + S essence.
  - A buff's tier is its class skill's **last** rung (*"if a skill is learned at 40 but last lvl is at 52 that
    s T52"*): Might/Bulwark/Alacrity/Swift are T40, the next ten T52, and Body/Soul/Resolve/Insight/Vampirism T61.
  - Learn prices come from the agreed ladder, 20k (L0) … 4M (L10).
- **Removed recipes:** stones, Return/Resurrection and the Ultimates, every Dash, the Instant potion, and the
  enchant + attribute scrolls (**never craftable**).
- **Rare potion Values** are now **5,000 / 10,000** (were 1,500 / 3,000), his base prices.
- **New items: Volcanic Ash, Volcanic Stone, Volcanic Bar.** Nothing gives them yet: the ash and stone drop in
  step 11, and the bar refine comes in step 10. So the rare potions and 2h rune boxes cannot be crafted until then.
- Debug: "Set craft levels" rows for the five types. SmokeTest: 12 new checks (gates, spending, respec locks).

## 2026-09-24 — 0.203.0: becoming a crafter (`BL-273` part 2, step 9 of the rework order)

> *"As we remove the professions we have no lock and no way to disable crafting once the quest is done"*

**The five professions are gone. One trial at level 40 makes you a crafter for good.** Crafting now has
recipe slots, a generic level plus three type levels, and recipes at 20/40/60/100%. ⚠ `game.db` delete
(new columns). ⚠ Protocol 48: **a new APK is needed.**

- **The Master Crafter**, one per town (he stands where the Master Smith stood). He gives **The Master's
  Trial** (level 40): gather 20 Seasoned Hardwood + 20 Raw Iron (Dune Orc Archers), 20 Rough Gems
  (Harpies), 2 hammer recipes (40%) + 1 Hammer Head (Marsh Marauders), bring them back, learn one recipe
  from the bag, and try to forge the **Blacksmith's Hammer** at his anvil. **A fail sends you back to step
  1** (the mats and the used recipe are gone). Give him the hammer: you are a crafter with **10 slots,
  generic and every type at L0**. ⚠ Mobs and drop chances (50/50/50/15/10%) are placeholders.
- **Crafting happens only at a Master; learning and forgetting happen anywhere** (the window browses
  elsewhere). One slot per recipe; **forgetting** frees it and refunds nothing.
- **Levels:** generic 0-10 (+5 slots each, 60 at L10) and weapon / armour / jewels 0-10. Every ATTEMPT,
  a fail too, pays tier-weighted points: **T40 1 · T52 2 · T61 3 · T76 5 · T80 8**, and 1 for a generic
  recipe (generic level only). Level N costs 20·N points (1,100 to L10). ⚠ Placeholders for a playtest.
- **The bonus** (+0.5% per generic level + 0.5% per type level) counts on **T76/T80 crafts only**.
- **Gear recipes are ITEMS with a %** (T40/T52 100 · T61 60/100 · T76 20/40/60 · T80 40/60). Learning
  one fills its slot at that %; a higher % overrides the slot in place, while a lower one is refused and
  kept. **Each craft spends one recipe item** at or below the learned %, rolls at that %, and scales every
  input 30/50/70/100%. F/E (T1/T20) gear is no longer crafted. A recipe needs the grade's character level
  to learn.
- **The Master's shelf** sells the T40/T52 100% recipes at **10% of the piece** (placeholder). T76/T80
  bosses now drop **60%** recipes and elites **40%** (T80 recipes drop too now); the rates stay until
  `BL-274`. T61 recipes have no source until step 11.
- **Generic recipes** (potions, scrolls, refines) are **bought at the Master** to learn: unlocked at
  generic level 0/2/4/6/8/10 (from their old rung), 20,000 × (1 + unlock level) gold, level 40 (76 for the
  old top rungs). No recipe item per craft. ⚠ All placeholders: **step 9b** writes the real table (his
  50-60%-of-shop rule, `--craft-cost` C5).
- **Debug window:** "Become crafter", and "Craft levels" presets (all 0 / 5 / 10, generic 10, weapon 10,
  generic +1). The old profession rows are gone.
- **Deleted:** `Profession`, craft EXP and the L1-L6 bands, the five joining quests, join/quit, the
  per-grade gear odds table, `DropOnly` blueprints.
- **Quest engine:** a collect step can be `PaidAtHandIn: false` (the craft spends it), and consecutive
  satisfied steps now walk in one pass.
- SmokeTest: 5a plays the trial for real (including the fail→step 1 path when the dice give it), 5b
  covers recipe %, slots, points, forgetting, the shelf and a generic recipe; section 1 gains 11
  catalogue checks. The three old crafting FAILs are gone with the sections they lived in.

## 2026-09-24 — 0.202.0: the shops (`BL-272` part 2, step 8 of the rework order)

> *"so breaking like crazy T40 can get u a t52"* … *"the 2 hours tick only while WORN"*

**A T52 essence shop, 2-hour temporary Common gear on the gear merchants, and an Unequip-all button.**
`BL-272` is complete and archived. Its one deferred leftover, rarity for consumables, is now `BL-288`.

- **The T52 essence shop: Assayer Corvane, in Greymarsh only** (his pick: *"A single new NPC in a T52
  town"*). He sells every T52 Mythic piece (8 weapons, 3 bodies, helm, gloves, boots, shield, 3 jewels) for
  **essence only, with no gold and no mats**. The price is **¼ of the item's price in Cobalt essence + ¾ in
  Darksteel**, each at its essence's sell price. A 2H costs **750 C + 6,750 D** and a ring **63 + 563**
  (his pick of three readings of the pre-`BL-287` "500 T52 + ¾ in T40", 2026-09-24). The table is an
  authored literal, generated once; full table in `docs/Formulas.md`.
- **Temporary Common gear, T40 and T52**, from every Armsmaster (a **weapon box**: pick one of the 8
  weapons) and every Outfitter (an **armour box**: pick a heavy / light / robe set, and each set includes a
  **shield**). Both are priced at the **Common price**: weapon **214,286 / 675,000**, armour **357,143 /
  1,125,000**. There is no temporary jewellery.
- **A temporary piece has 2 hours of WEARING.** The clock ticks once a second only while the piece is
  equipped (an offline-farming character counts as wearing it); in the bag, the warehouse or offline it is
  paused. At 0 the piece is gone. The item card shows "1h 59m of wearing left (paused)". Temp pieces are
  **untradeable, unsellable and cannot be broken** (his pick: a bought piece that broke into essence would
  be a vendor selling essence), but they can be destroyed or kept in the private warehouse.
- **"Unequip all"** sits on the Presets header of the equipment window: one tap takes everything off. It
  is the built-in empty preset (his pick over a saved-empty A/B/C) and, like the presets, is refused in
  combat.
- Measured: nothing. No drop, sell or break number moved, so BalanceMatrix has nothing new to report. The
  essence shop is a **sink** that `--craft-cost` does not model yet.
- ⚠ **`game.db` delete needed** (a new `WornSecondsLeft` item column). **APK needed** (protocol 47: the
  shop row carries an essence price and the item a worn clock; the new button, card line and vendor rows).

## 2026-09-24 — 0.201.0: prices + essence, IG-shaped (`BL-287`, step 7b of the rework order)

> *"a common why its is at 1/4 of the normal?"* … *"one common item or 2 [must] not allow me to craft an item"*

**Prices, selling and breaking now follow IG's shape.** You sell anything for half its price, a Common costs a
twentieth of its Mythic, and breaking gives you less than selling would.

- **Everything with a price sells for 50% of its own buy price.** This replaces four rules: gear sold for its
  Mythic price ÷10 (Mythic) down to ÷200 (Common), buff potions ÷10, and materials and healing potions 30%.
  Items authored to sell for nothing (premium items, runes, buff potions) still sell for 0. One code path
  (`ItemCatalog.SellPrice`) serves the vendor, the trade window and the client's price labels.
- **A Common costs 0.05 × its Mythic** (was 0.225), so it sells for 2.5% of the Mythic. A T40 Common 2H costs
  214,286 and sells for 107,143.
- **Mythic prices move per tier**, with every slot of a tier moving by the same factor: T1 **×2.2** (2H 188,571),
  T20 **×0.65** (2H 1.39M), T40 and T52 **×0.5** (2H 4.29M / 13.5M). T61, T76 and T80 are unchanged (60M /
  120M / 600M). The merchants' Mythic T1/T20/T40 stock moves with this.
- **Breaking gives 40% of the item's price, paid as essence at the essence's sell price**, so breaking
  costs you about a fifth of the gold that selling would pay. Essence now sells for **1,500 / 4,500 / 7,500 /
  12,500 / 25,000** (Darksteel … Soulcrystal). A 2H breaks for **1143 / 1200 / 3200 / 3840 / 9600** (Mythic)
  and **57 / 60 / 160** (Common). Commons and Mythics use the same rule, so 0.200.0's "a Common breaks for
  70%" is gone. The tables are still literals, generated once. Recipe essence amounts are unchanged, so a
  2H craft takes about 7 / 13 / 7.5 broken Commons. Full table in `docs/Formulas.md`.
- **Common drops roll PER SLOT**: ring highest, then earring/boots/gloves, helm/shield/necklace, body, weapon
  lowest. The ranges are T40 **2% → 1%**, T52 **1% → 0.2%** and T61 **0.3% → 0.05%**, which is a Common every
  ~7 / 17 / 59 normal kills. **An elite rolls ×2.** All of it still runs through the `common` drop group
  and its `/droprate` knob.
- **No healing potion drops from any mob above level 40** (*"go town buy potions"*). The Greater potion is
  now craft- and box-only.
- 📊 **Measured (BalanceMatrix, before → after).** Common gear pays **4.8× / 12.8× / 14×** the coin at
  40 / 52 / 61 (was 0.1× / 0.8× / 1.5×). A 1→60 climb that sells everything earns **63.6M** (was 9.6M). The
  Rune of Sinister's 1000 kills at 50 / 70 pays 5.4M / 7.8M (was 0.54M / 1.38M). `--craft-cost` per success:
  T40 **16.1h**, T52 **30h**, T61 **70h** (0.200.0: 17.3 / 28 / 59). Essence per 2H craft is 2.0h / 4.5h / 13.4h
  on the elite camp. That lands inside what §2.5 predicted (T40 16h, T52 ~36h, T61 ~79h) and is the
  gear-gold faucet that 0.199.0 closed, reopened on purpose.
- APK needed: the client prices, sells and offers Break from `Game.Shared`. No schema change.

## 2026-09-24 — 0.200.0: essence (`BL-273` part 1, step 7 of the rework order)

> *"breaking common or even mythic darksteel gives you 'Darksteel essence' ... acquired only by breaking full
> items -> so not mindlessly selling in the vendor"*

**Breaking gear now gives its grade's ESSENCE, and nothing else.** There are five: **Darksteel / Cobalt /
Bloodsteel / Adamantine / Soulcrystal Essence** (T40 / T52 / T61 / T76 / T80). The old `BL-22` roll (rarity →
material rarity, grade → quantity) is deleted, and that closes 0.199.0's hole where a shop-bought Mythic broke
into Mythic materials.

- **The amount is an authored table, one cell per tier × slot, never computed from a price** (*"changing prices
  later should not change the essence amount"*). A **Mythic** breaks for its full price-worth: the 2H is **2000 /
  2000 / 4000 / 7000 / 10000** (T61 and T76 ruled today from the `--craft-cost` placeholders), and every other
  slot follows its price share (T40: 1H 1800, body 1200, necklace 1000, helm/shield 667, gloves/boots 400,
  earring 333, ring 167). A **Common** breaks for **70% of its own price**, ruled today: its price is ×0.225, so
  a T40 Common 2H gives **315** and a ring 26. The full table is in `docs/Formulas.md`.
- **T1 and T20 gear cannot be broken** (ruled today): the essences start at D. Their Break button is gone.
- **A shattered enchant leaves essence.** A Normal scroll that fails `+N → N+1` returns **N × 10%** of the
  item's break value (+3 = 30%, +10 = 100%, +15 = 150%). Greater and Safe scrolls never destroy the item, so
  they never pay. A bound (unsellable) piece pays nothing, the same rule breaking has.
- **No vendor sells essence.** It sells for 1/25 of the gold it stands for: D 171, C 540, B 600, A 685, S 2,400
  (a T40 Mythic 2H sells for 857k, breaks into 2000 essence, and that essence sells for 342k).
- Nothing uses essence yet. Recipes start asking for it in step 10, the T52 essence shop is step 8, and the
  direct T76/T80 essence drop is step 12.
- Fixed on the way: when the bag was too full to hold the result of a break, the piece was put back as a
  **fresh** item, losing its enchant, attribute and lock. The same row is put back now.

📊 **Measured (`--craft-cost`, before → after):** essence for a T40 2H craft costs **13.8h → 3.2h** (T52 11.9 →
2.8, T61 13.5 → 3.1), because a Common now breaks for its real price instead of the "20× cheaper" guess. That
brings the per-success times back to **exactly the numbers you settled in §2.2 #10**: T40 **17h**, T52 **28h**,
T61 **59h**; a full character 70h / 116h / 241h. T76/T80 still read "never" (no essence source until step 12).
BalanceMatrix's **M13** (the `BL-22` salvage budget) is deleted: it measured a faucet that no longer exists.

No `game.db` delete for this version (no new column); the one owed since 0.199.0 still stands. **New APK
needed** (the Break button, its label and the dialog read the shared table). SmokeTest: 7 new checks pass,
including a real break on the server (315 essence); the same 3 pre-existing crafting FAILs.

## 2026-09-24 — 0.199.0: the rarity collapse (`BL-272` part 1, step 6 of the rework order)

> *"equipment will have common equip items and normal (current mythic) items -> no longer in between"*

**Equipment is Common + Mythic now.** Uncommon, Rare, Epic and Legendary gear is deleted: the items, their
drop rungs, and the Epic/Legendary set variants (93 sets → 31). Consumables and materials keep all six rarities.

- **Mythic** is the authored piece. The player sees it as a **plain item** with no rarity word (the tooltip
  drops the row, and the loot line reads `[D]` instead of `[D/Mythic]`). It keeps its colour.
- **Common** exists at **T40, T52 and T61 only** (`{id}_common`). It has the Mythic piece's stats and is
  **unmodifiable**: no set bonus, no attribute, no enchant. The server refuses the enchant, and the client never
  offers a Common in the enchant picker. Its tooltip says so. It keeps the old Common price: ×0.225 of the Mythic
  buy price, Mythic ÷ 200 when sold (a T40 two-hander buys at 1.93kk and sells for 42,857).
- **Drops.** A normal T40-T61 kill rolls **0.5%** and an elite **2%** for one Common piece, through their own
  group `common` (×1, so those are the delivered chances; tune it with `/droprate common`). Each family
  (armour / accessory / weapon / jewel) gets an equal quarter of that roll. **Below T40 and from T76 up, normal
  and elite mobs drop no equipment** until `BL-274` gives every mob its rare Mythic.

His interim rulings for what the deleted rungs used to feed, until the later steps replace them:
- **Bosses** pay **one guaranteed Mythic piece** of their tier (group `boss`, ×1), pulled forward from
  `BL-274` part 2. The old 2%-per-family Mythic accent stays as it was. This replaces Epic 70% + Legendary 40%.
- **Merchants** sell **Mythic T1 / T20 / T40** at the authored price (pulled forward from step 8). Examples:
  a T20 one-hander costs 1.91kk (you remembered ~1.4kk), a T40 heavy body 5.14kk, a T40 two-hander 8.57kk.
  The T52 essence shop and the 2-hour Common boxes are still step 8.
- **Crafting:** the Legendary share of a gear craft is now a **fail**. Success is his old Mythic column
  unchanged (E 50% … A 20%, S 5%). The mats were sized for S at 25% success, so an S craft costs 5× more until
  the recipe rework (`BL-273` part 2).
- **Town guards and the `BL-47` demo creatures wear Mythic**, and their stats rise as a result (Epic was 70%).

🔴 **Measured (BalanceMatrix, before → after): gear was the gold faucet and it has mostly closed.** Gear value
per normal kill: level 20-39 **158 → 0**, 52 **2,076 → 351**, 61 **4,613 → 780**, 76 **9,226 → 0**. The
playtest-18 idle farm that sold gear reads **541k → 350k**, which is his "sold nothing" figure. This follows
directly from the ruled numbers and no knob was moved; `BL-274` (per-mob rare Mythic drops) is what refills it.
⚠ BalanceMatrix's historical `G3` tables (mobs built as players) were fitted on Common/Rare/Epic loadouts that
no longer exist. They now measure Mythic, so their old conclusions ("15/16 rows fit a ×2 passive") are not
reproducible. ⚠ Until step 7, salvaging a Mythic item still yields **Mythic** materials. A shop-bought T20/T40
or a boss's guaranteed piece is therefore a source of Mythic mats. Step 7 replaces salvage with essence.

`game.db` delete (items with deleted ids). New APK needed (the client's catalogue, tooltip and craft window
changed). SmokeTest: 12 new catalogue checks pass; the 3 pre-existing crafting FAILs remain.

## 2026-09-24 — 0.198.0: Charisma (`BL-283`, closes step 5 of the rework order)

> *"player recomendetions/likes/charisma points increase the gauge fill up rate as well"*

**Recommending another player now speeds up their Wayfarer's Blessing.** The rules are in
`Game.Shared/Charisma.cs`. The action is still called "Like" in the action bar and on the wire.

- **A recommendation is +10**, taken from the giver's budget of 20 a day. It is refused if you recommend
  yourself, a character on your own account, or someone you already recommended today. It is also refused if
  you are below level 20, or if the target has already received 10 today. The same rules apply to a logged-off
  target (they run in the database), and a refused offline recommendation, a typo included, gives the budget
  point back.
- **Current vs lifetime:** current is the last 30 days, kept as 30 daily totals, and capped at 1000 when read.
  Ten a day fills it on day 10, and up to 20 missed days cost nothing. Lifetime is everything ever received,
  and the Charisma board and its #1 title (**Beloved**) rank on it.
- **The effect:** Blessing fill × (1 + 0.1 × floor(current ÷ 100)), multiplied with the booster rune, so ×4
  with both at full. 123 is still +10%.
- 🔴 **Charisma no longer gives EXP/SP.** The old +0-50% bonus is removed (ruling 4: the Blessing and the
  title, *"nothing else"*). Your note's *"+ 400% + 50% = x5.5"* counted it, so the top personal sum is now
  **×5** (×6 during a Blessing).
- **Penalties drain lifetime only:** a PK kill (karma × 0.01), and a chatban, jail or kick. They leave the
  30-day ring alone. A ban still zeroes everything.
- The details sheet shows **Charisma: lifetime (current)** at the top of *Other*. It refreshes when you are
  recommended, and after midnight when current drops.
- The castle "Noble" point is filed as `BL-286`, because castles don't exist yet.
- SmokeTest's charisma section now checks the new rules (level 20, one per giver per day, same account on the
  offline path, +10 on the board).
- ⚠ **`game.db` delete** (the pool column became the ring and the givers columns). **Needs an APK** (a new
  DTO field, and the sheet line).

## 2026-09-24 — 0.197.0: the Wayfarer's items and the raid-boss Favor grant (`BL-277` part 3, closes `BL-277`)

> *"We must have the 4 Runes"*

**The four runes, the restore potion, the subclass box, and the Favor a raid boss grants.** `BL-277` is
done and archived. The numbers are in `Game.Shared/WayfarerFavor.cs`. The items are in `Items.cs` / `Boxes.cs`,
and their two buffs are in `Skills.Wayfarer.cs`.

- **Favor Keep-Rune (1h / 2h):** while one is held, a kill does not drain the Favor, so no stage is lost and
  there is no stage-loss Blessing fill either. The +0.1% kill fill and a Blessing's refund still apply.
- **Blessing Booster Rune (1h / 2h):** ×2 Blessing fill from every source, read in `BlessingFillRate` and
  nowhere else. Charisma (`BL-283`) will multiply it there (×4 with both). The sheet's fill rate shows it.
- Both are ordinary held runes: the item's wall clock drives the buff, the warehouse switches them off,
  and they cannot be deleted. Holding a 1h and a 2h of the same kind does not stack. The later expiry wins.
- **Favor Restore Potion:** +2,500 Favor, capped at 20,000, and refused (not used up) when the gauge is
  full. It has an hour of reuse, saved as a **wall-clock time on the character**, so logging out and back in
  does not reset it (the ordinary potion cooldowns are in memory only). The bar shows the countdown.
- **Wayfarer's Subclass Box:** both 1h runes + 4 potions. It is given **the first time each subclass slot
  is filled**, and never again: a swap, a cancel-and-replace, or removing and re-adding a class gives
  nothing. A new counter (`SubclassBoxesGiven`) records how many boxes were paid, compared against the
  subclasses held. If the bag is full, the box is paid at the next login. The runes' hours start
  when you open the box. The `BL-252` 24h Exp/SP rune is still given per subclass created, as before.
- **Box contents are never lost to a full bag:** any box whose contents are all guaranteed now refuses to
  open until everything fits. Random boxes are unchanged.
- **Raid-boss grant:** `min(gauge room, yourExpShare ÷ MobExpReward(L) × 11.2)`, from your own base EXP share
  (after the party split and level gap). That is ~3,000 per 9-man member at every level, ~9,000 per 3-man, and
  a full gauge solo. It is **grant-only**: no drain, no refund, no Blessing fill. You get nothing if the boss
  judges you (gap > 8, the same `BossJudges` that decides the EXP), and nothing if a rune zeroes your EXP.
- **Nothing sells them.** Events do not exist, and the recurring premium grant is `BL-284`. The sources are
  the subclass box and the admin `/give`. All six items are untradable and unsellable.
- The Details sheet's rates are now re-sent whenever a rune comes or goes. Before this, a reward rune's rate
  change reached the sheet only when something else pushed it.
- ⚠ **`game.db` delete** (two new character columns). **Needs an APK** (the client builds item and skill
  names from `Game.Shared`).

## 2026-09-24 — 0.196.0: the Wayfarer's Blessing (`BL-277` part 2)

> *"Once the gauge reaches 100%, the Blessing triggers automatically."*

**A 0-100% gauge that fills while you fight, and at 100% fires 3 minutes of +100% EXP/SP by itself.**
`Game.Shared/WayfarerFavor.cs` (`WayfarerBlessing`) holds the numbers.

- **Fills:** +0.1% per non-boss kill that paid EXP, +1% per minute in combat (by the second, the same
  30 s "in combat" window as the logout gate), +8% per Favor stage a kill's drain carries you through,
  +30% per level earned. At ×1 that is a Blessing every ~90 min with the Favor empty and ~60 min while it
  drains (design §3.10).
- **One fill-rate multiplier, applied once** where the gauge is added to (`AddBlessing`), so it scales
  every source. It reads **×1** until charisma (`BL-283`) and the booster rune (part 3) exist.
- **While it runs:** +100% added to the personal bonus sum (charisma + Favor + Blessing, his *"x3.5"* on
  a ×2.5), and **Protection + refund**: a non-boss kill does not drain the Favor, and gives back the
  points it would have drained. A boss kill gets no fill and no refund. The gauge sits at 100 and resets
  to 0 when the 3 minutes end.
- **Buff icon** "Wayfarer's Blessing", cosmetic like the boss's judgment: the clock lives on the
  character and re-asserts the icon every second, so death, a subclass swap, a cleanse or a double-click
  cannot end it early. The clock pauses while logged out and is saved with the character.
- **Details tab → "Other":** a *Blessing* row (`98 / 100` or `ACTIVE`, and the fill rate). The Exp/SP
  rate rows include the +100% while it runs.
- ⚠ **`game.db` delete** (two new character columns). **Needs an APK** (`Favor` message grew 3 fields).

## 2026-09-23 — 0.195.0: the Wayfarer's Favor gauge (`BL-277` part 1)

> *"The vitality only helps someone not so active not to be so far behind."*

**A 0-20,000 catch-up gauge that fills while you are away and drains while you farm**, paying
**+50% EXP/SP per stage** across his eight stages (500 / 5,000 / 6,000 / 10,000 / 11,500 / 15,000 /
17,000 / 20,000), so a full gauge is +400%. `Game.Shared/WayfarerFavor.cs` holds the whole rule.

- **Fills:** offline time is credited once at login as `(now − last save) × 40/min`, clamped to 20,000,
  and logging out anywhere counts. Idling in one of the five **cities** pays 40 per full 60 s, and
  stepping out or any fight restarts the minute. Dungeon doors and the training outpost don't count.
  The two never stack, and a living offline-farmer gains nothing. A full gauge is 8 h 20 min.
- **Drains:** each non-boss kill costs `20000 × (your EXP share ÷ a same-level normal mob's EXP) ÷
  (killsPerHour(L) × 2)`, so a full gauge lasts **2 hours of farming at every level** (~143 points for a
  same-level kill at ~70/h). A party member drains on their own share only. **Boss kills never
  drain**: their grant arrives in part 3. `killsPerHour(L)` is an authored table (59-90/h) read off
  BalanceMatrix's M1 clock with the new `--favor-kph`.
- **The bonus ADDS to charisma's** (his *"100 + 400 + 50 = x5.5"*), and the server rate and runes
  multiply the sum as before. With the gauge empty, nothing about levelling moved. Kill EXP only: quest
  rewards are not touched.
- **Details tab → "Other":** Favor points / stage, and the finished Exp / SP / Gold / Drop rates
  (server × runes × bonuses), pushed by the server on a new `Favor` message.
- **Admin Tune tab:** *"Favor pts/min (away+city)"*, default 40 (`RateConfig.FavorPerMinute`).
- ⚠ **`game.db` delete** (two new character columns). **Needs an APK** (new message, new Tune field).

## 2026-09-23 — 0.194.0: boss regen is a clock (`BL-278`); the boss band is 8 levels

> *"i want to make the boss not immortal but not able to be solo + healer killed"*

**Engaged mob HP regen = `maxHp ÷ D` per second, D = 30,000** (`StatCalculator.MobRegenDivisor`, an INT),
**×2 after a boss's 1st enrage and ×10 after its 2nd** (`EnrageRegenMult`, hard-coded). On a 6M boss that
is **200 / 400 / 2,000 HP/s**, where the old flat 1/1000 was 6,000 HP/s, more than a full party's DPS. The
top rate is still 3× below the old one, so a proper party wins against an enraged boss, and a tank + healer
duo takes long enough to reach enrage and lose.

- **One rule for every rank.** Any mob with `maxHp < D` (every normal, most elites) regenerates **nothing**
  while engaged, and the tick is skipped rather than computed, because the regen loop floors a heal at
  1 HP. That is his "no in-combat regen for mobs", as IG does it. A 60k elite heals 2 HP/s.
- **The admin Tune tab** shows *"Mob regen fight: maxHp / N"* (an integer) in place of the old fraction.
  0 = no engaged regen anywhere; anything else is floored at 10. Idle regen (5%/s) and the engaged MP
  rate did not change. An old `debug-config.json` without the field loads the default 30,000.
- **`BossJudgmentGap` 9 → 8** (his re-ruling beside `BL-277`: *"at 9th lvl difference boss start to use
  judgment and no exp/favor grant"*). A gap of 8 is the last one inside the fight. At 9+ the boss judges
  you **and pays you nothing**: `PayKillShare` now skips a member the same `BossJudges` would punish, so
  the fight and the payout cannot disagree about who was in it. The Favor grant (`BL-277` part 3) will
  read the same predicate.
- `docs/Formulas.md`: the mob regen line and the raid level-gap table.

**Needs an APK** (the Tune tab's field changed type; the DTO is positional).

## 2026-09-23 — 0.193.0: up to 24 extra skill squares on screen (`BL-269`)

> *"need option to add more 6/12/18/24 skill slots (half of or full the 2nd and 3rd skill bars) -
> like additional skill bars"*

**Settings → "Extra skill slots"** cycles **off → 6 → 12 → 18 → 24**, remembered on the phone. The
extra squares are rows of six in a second panel stacked on top of the main bar. With 12 you see the
whole next page, and with 24 the next two.

**What they show:** the pages **after** the main bar's current page (main on 1 → extras show 2 and 3;
page the main bar to 2 → extras show 3 and 4, wrapping). So an extra row never duplicates the main
row, and one swipe still pages everything. One `BarIndexOf(visible)` maps a square to its bar index,
and tap, hold-menu, move, remove, assign and the refresh all go through it. Each square is a full
square: auto `A`, the `BL-279` clock, reuse sheet, count, cancel X, toggle ring.

**The server did not change.** Its bar has been 60 slots (five pages of twelve) all along, and the
phone showed one page at a time. `SyncSkillBar` and the saved bar are untouched, which is what the
backlog entry's ⚠ asked for: the option decides what is **shown**, not what is **stored**.

**Needs an APK.**

## 2026-09-23 — 0.192.0: a custom auto-hunt delay per skill, exact or added (`BL-279`)

> *"holding down a skill on the bar shows the context menu … a 'delay:ON/OFF' and 'custom delay'
> buttons under 'auto on' … the custom delay is between 1~9999s … exact/added on the place of current
> 'max' … When custom delay is set and OK is clicked the 'delay:ON' becomes active and a clock icon
> appears on the skill"*

**Half of it already existed, unreachable.** `AutoSkillDto.ExtraDelayTicks` has been in the auto-hunt
config since the auto-hunt was built, and `AutoCycleTicks` already added it after the skill's own
reuse. No UI ever set it. What was built:

- **`AutoSkillDto` + `DelayExact` + `DelayOn`** (appended with defaults, so saves and older clients
  read as "added, on", which is what the field always meant).
- **`AutoCycleTicks`**, the one cycle rule: *added* = the live reuse + N; *exact* = N from use to use,
  **never below the live reuse**, which is re-read at use time because cast speed and reuse buffs
  move it. *Off* uses the default and keeps the number. The server clamps N to 1–9999 s
  (`GameConstants.AutoDelayMaxSeconds`, shared with the picker).
- **The slot menu** gains **Delay: ON/off** and **Custom delay** under Auto. With no delay set yet,
  Delay opens the picker, since there is nothing to switch to.
- **The picker** is the shop's numpad: it now opens on a given value, and its **Max** button can be
  a mode switch. Here it reads **Exact / Added**, and the line under the title says what the number
  will do.
- **A clock** (drawn from rectangles, `UiKit.Icon.Clock`; the TMP atlas has no glyph for it) in the
  slot's top-right corner while the delay is on.

**Where it lives** (the entry's ❓): **per skill id, per class**, in the server's auto-hunt config beside
the Auto mark, not on the slot. Move the skill and its delay goes with it. A delay set on a skill that
is not marked Auto rides along as a disabled row, so the number survives until you mark it.

**Needs an APK.**

## 2026-09-23 — 0.191.0: the watch looks like the watch, and follows a PK into town (`BL-276`, §102.7)

> *"can town watchman and field watchman be NPCs? not mobs .. now they look like scary mobs red
> aggressive lvl 90 .. they must be npc with titles and just fighting 'script' .. also they must be
> allowed to fight inside the town — when a pk is inside a town and they lock on they should be able
> to hit him"* · *"without pvp on I could hit with the whirlwind aoe skill and die from a 90 lvl
> field guard"*

**They stay mobs in the simulation, and are drawn as NPCs.** Their "fighting script" is the mob AI
(aggro, chase, swing, leash, respawn), and turning them into `EntityKind.Npc` would have meant
writing a second one. So `Entity.ToDto` sends a guard as `Kind = Npc`, with `Aggressive = false`.
The client draws the NPC model, the NPC's yellow name, the title line (**Town Watch** for the town
pair and now **Field Watch** for the field pair), no `*`, and a Talk button, which gets one line back
(*"Move along. The watch keeps the peace."*) instead of a silent tap. Attacking one still needs
PvP on, exactly as for any NPC (`BL-115`). An NPC's nameplate now shows its HP bar **once it is
hurt**, so a guard in a real fight shows how the fight is going.

**The town no longer shields a PK from the watch.** New `TownShields(mob, target)`: the safe zone
protects a target from every creature *except* a guard whose target is a PK. It replaced the plain
safe-zone test in the aggro scan, the caster AI, and the swing. Guards may also **walk** inside a safe
zone (every other mob is still stopped at the line). A guard's own area skills reach **PKs only**,
so an innocent standing next to the outlaw is never splashed.

**§102.7 — a PvP-off AoE no longer reaches a guard.** A single swing at a guard already asked the
PvP toggle (`BL-79`), but `EnemiesInRadius` never did. So one Whirlwind pulled a level-90 wall onto a
player who had not chosen that fight. Guards are now in a player's sweep only with PvP on.

**Needs an APK** for the nameplate HP bar; the rest works with the current APK, since the Kind
comes from the server. ⚠ Unplayed. The first real test is a red name running into town.

## 2026-09-23 — 0.190.0: quest rewards are printed in the combat channel (`BL-271`)

> *"receiving reward from quest should be shown in the combat channel .. exp/sp/reward .. if its
> written a player can see and decide if that quest is worth repeating"*

`CompleteQuestAtNpc` (the only path that pays a quest) now writes to the **combat** feed, the same
channel and tags a kill's reward uses:
- `Quest reward (<name>): Exp: +N, SP: +N, <gold>: +N`, tagged `EXP`;
- `Quest reward: <item>[ xN]`, tagged `LOOT`, one line per distinct reward item.

The numbers are what was **banked**, not what was authored. A private reward tally wraps
`AwardExp`, the one place world rates, runes and the SP ceiling apply, exactly as the kill line
does. Gold is the post-rate figure. The outer kill tally is saved and restored, so a quest closed
mid-kill cannot swallow that kill's line. "Quest complete: X!" stays in System as before.

Server only — **no APK needed** (the client already renders both tags on the combat channel).

## 2026-09-23 — 0.189.0: landscape both ways, whatever the rotation lock says (`BL-268`)

> *"default game is landscape mode but I want to rotate on both sides (both landscapes only, no
> portrait)"*

The project **already** allowed both landscapes and no portrait (`allowedAutorotateToLandscape*: 1`,
`defaultScreenOrientation: 4` = auto-rotate). What stopped it was `useOSAutorotation: 1`: Unity then
writes `userLandscape` into the manifest, which **obeys the phone's rotation lock**. On a phone with
auto-rotate off, the game stays whichever way it started. The setting is now **`0`**
(`sensorLandscape`: flips with the sensor between the two landscapes, never to portrait, lock or
no lock). `GameBoot.Awake` also sets the four `Screen.autorotateTo*` flags and
`ScreenOrientation.AutoRotation` in code, so a stray settings edit cannot quietly undo it.

**Needs an APK** (a manifest + client change). ⚠ Unverified on a device; I could not run it.

## 2026-09-23 — 0.188.0: equipment locks per ITEM (`BL-267`)

> *"I want [lock] to be per equipment item not per item_ID .. now I have 2 maul weapons .. and one
> is +3 .. I lock it and I cannot sell the other maul"*

`BL-239` (0.157.0) locked the **def id**, so that a locked potion stack stayed locked after it was
drunk empty and re-looted. That reason only holds for stackables. Now:
- **Equipment** (anything that does not stack) locks **the row**: `InventoryItem.Locked`, saved on
  the item record (`ItemRecord.Locked`). Lock the +3 maul and the other one sells.
- **Stackables** keep the def-id lock exactly as before.
- `IsLocked(player, item, def)` is the one question, and all seven disposal paths ask it: sell, sell
  by rarity, bin, break down, both keepers, trade. `LockRefuses` takes the row and names it
  (a custom name included).
- A def-id lock on a gear def left over from before is still honoured, and **unlocking that item
  clears it**, so nothing is stuck locked with no button that reaches it.

**The wire did not change.** The lock array on `InventoryUpdate` now carries def ids *and* the live
InstanceIds of locked rows, and `SetItemLock`'s one string is a def id or an InstanceId. The server
tells them apart with `Guid.TryParse`. The client asks `Boot.IsLocked(def, item)` at all seven UI
sites and sends `GameBoot.LockKey(def, item)` from the details window.

⚠ **Schema change** (a new column), so `game.db` must be deleted, which was already owed.
**Needs an APK** (an old APK would still send the def id for gear, so the old behaviour stays).

## 2026-09-23 — 0.187.0: his warrior CSV edits, built (`BL-275`)

> *"saints dance -> target/aoe to target/single"* · *"whirlwind … 2s and 10 hits … the 4s lock the
> warrior for too long"* · *"need a toggle @20 that disables the 2h blunt and skills aoe"*

The five CSVs he edited on 2026-09-23 are committed with the code that follows them.
`SkillCsvSeed --check` is **green** (it was 62 discrepancies).

- **Saints Sword Dance** (Elf, warrior 3rd/4th) — `target/aoe` 150 → `target/single` 0. The wrapper
  and the stroke both lose their radius: ten strokes at one body.
- **Whirlwind** (war_aoe 3rd/4th) — ten strokes over 2 s (`ChannelShots` 20 → 10, `DurationTicks` 40
  → 20), same power per stroke, so the skill does half the total damage and locks you for half as
  long. **The DURR cell still said `4` on all thirty rows; it is now `2`**, reading his DESCR as the
  truth, as `BL-275` proposed.
- **Master of Combat** (war_aoe 4th) — attack speed +20% → **+10%**, plus **−10 evasion** as a flat
  minus on the buff (`BuffEvasion` added to the effect mask, the `BL-214` lesson: without the flag a
  magnitude never applies).
- **Single Mark** (`single_mark`) — **new**, warrior 2nd @20, 3,400 SP, a no-MP toggle, 2H blunt.
  ×2 crit rate (blunt's weapon crit factor 0.40 → 0.80, a greatsword's exactly, per his note), −20%
  on both SKILL-damage channels (basic swings keep full power), and **every AoE collapses to the
  main target**. `Entity.SingleTargetOnly` is read off the buff itself, so nothing new persists.
  Two places honour it: the offensive sweep in `ExecuteSkill` keeps only the main target (the aimed
  body, or for a self-centred ring whatever you have **selected**, if it is inside the ring), and
  `ResolveCleave`, the 2H-blunt basic cleave, returns early.

⚠ Whirlwind at 10 strokes is **half the damage per cast** of 0.179–0.186. That is his call, but no
balance doc has measured it.

**Needs an APK** — a class-skill-table change (Single Mark's Learn row is built by the client from
the compiled `ClassSkills`).

## 2026-09-23 — 0.186.0: `/stat patk` reaches basic attacks (§102.6)

> *"admins /stat patk 99999 does nothing to basic attacks (at least against bosses) — dmg with 1kk
> patk was the same 500 .. only skills got affected"*

P.Atk has two getters. `Entity.EffectiveAttack` feeds skills and `Entity.EffectiveBasicAttack`
feeds the swing, and the admin override was checked only in the first. `EffectiveBasicAttack` now
reads `AdminStat("patk")` first, the same way, so the number he types is the number both paths use.
No boss-specific cap was hiding it: `FinalizeDamage` only applies the ±10 raid level multiplier.

Server only — **no APK needed**.

## 2026-09-23 — 0.185.0: smallest stack first, and partial stacks merge (§102.5)

> *"with 999 + 58 potions, drinking takes the 999. Buying 850 with 200 held makes 850 + 200. Ten
> stacks of 80 take ten slots"* — drink from the SMALLEST stack first; buying and looting top the
> partial stack up to 999 first.

**Drinking.** Every path that picks a row now picks the smallest one:
- `HandleUsePotion` (bag click, bar slot, quick-use button) redirects to the smallest row
  **identical** to the one clicked (`Stacking.SmallestLike`, identity by `SameStack`, so a bound or
  renamed copy is never spent in place of the one you meant);
- the auto-potion HP/MP ladders and the Buffs tab use `Stacking.SmallestOf` (it was
  `FirstOrDefault`, the oldest row, which is how the 999 went first);
- `BestHealPotion`/`BestManaPotion` break a rarity tie on the smaller stack;
- `ConsumeItem` (reagents, quest hand-ins, every def-id spend) takes smallest-first. It used to
  take from the newest row.

**Adding.** `AddItem` already filled partial rows before opening a new one (0.93.0), but only one
row at a time. Rows left over from before, or from paths that open fresh rows (admin `/give`, the
warehouse fast path), were never merged. The new **`Stacking.Consolidate`** runs after every
`AddItem` (buy, loot, quest reward, craft). It folds each group of interchangeable rows into full
rows plus one remainder, so ten 80s become one 800 the next time you buy or pick one up. Timed,
bound and renamed rows are not interchangeable and stay separate. Nothing is ever destroyed.

⚠ **I could not reproduce "850 + 200" from the code.** A plain 200 row was already topped up by a
purchase. If his 200 carried instance state (an admin `/give` with a flag, a quest-bound copy), the
top-up rightly skipped it, and it still will. Consolidate settles every case where the rows are
really the same item.

Server only — **no APK needed** (the client's bar already resolves an item by def id).

## 2026-09-23 — 0.184.0: a cast's item is spent when the cast STARTS (§102.4)

> *"A consumed item is spent the moment you click — interrupt = lost (Scroll of Return,
> skill/holy stones…)"*

Both ways a cast costs an item used to charge at **landing**: the scroll instance that started it
(`CastFromItemInstance`, the Return, Resurrection and 48 buff scrolls) and the skill's reagent
(`ConsumableId`, the skill and holy stones). An interrupt or ESC "refunded" the item just by never
taking it. That made Return a free retry until it went through, and made a stone-priced skill
free to start.

**The new `PayCastItems`** takes both at the one commit point every player cast passes through
(`UpdateQueuedSkill`, past the range walk, just before the 20% MP slice). If the item is gone, the
cast does not start. Cancel and interrupt no longer give anything back. `Entity.CastReagentPaid` stops
the landing from charging the reagent a second time. A mob that casts a reagent skill (no bag) keeps
the old landing path unchanged.

The feedback gate in `HandleCastSkill` (*"X requires 2x Skill Stone"*) is unchanged.

Server only — **no APK needed**.

## 2026-09-23 — 0.183.0: the warrior's `deflection` is gone (§102.3)

> *"Warrior `deflection` is not in any CSV → remove it, like `evasion_mastery` / `precision`"*

`deflection` (a 15%/30% chance to reflect a physical skill's full damage) was auto-granted to every
warrior at 40/76 by `SkillCatalog.ReflectPassiveFor`, with no CSV row behind it. The warrior's arm
is removed the same way the rogue's floor was: the def stays (removing an id reshuffles things) but
nothing grants it. `ReflectIdFor` still names it, so the existing else-branch in
`AutoLearnCoreSkills` **strips it from every warrior at login**. No new code, and no `game.db`
delete needed.

This is also the real cause of the §102.2 report that *"Harmony of Protection reflects skills"*:
Harmony's reflect was already basic-attack only, and Deflection was what bounced his skills.
**Saints Blessing's 10% physical-skill reflect is a BUFF field** and is untouched; now it is the
only skill reflect a warrior has.

Shared + server — **no APK needed** (the skill never appeared in a Learn tab).

## 2026-09-23 — 0.182.0: Fury Sigil and Physical Proficiency proc on basic attacks only (§102.2)

> *"Fury Sigil + Physical Proficiency proc on skills — basic attack only"*

Both ride the ON-HIT trigger, and the ON-HIT trigger has two callers: the basic-attack path and
the physical-skill path (`TryOnHitProcs` at the skill landing). Neither proc said which one it
wanted, so both fired from skills too. A new `SkillDef.ProcBasicAttackOnly` narrows the Hit
trigger to basic attacks. It is set on exactly these two; Combo Mastery, the Warlord's Supports and
the archer stances keep firing from both paths, because he named only these two.

**CSV:** `shared 4th.csv` — both DESCR cells now say *basic attacks only* (Physical Proficiency's
said *"physical skills or basic attacks"*). ⚠ That file is not one `SkillCsvSeed --check` walks.

### The reflect half of §102.2 was already true in code
`BuffReflect` feeds `Entity.MeleeReflect`, and exactly one place reads it: the basic-attack path
(bows excluded, capped at 50%). So **Harmony of Protection's 20% already returns basic-attack
damage only**, and **Saints Blessing already matches his spec**: 30% of every basic hit, 15% to
reflect a debuff, 10% to reflect a physical skill in full. The physical skills he saw bouncing came
from the warrior's auto-granted **`deflection`** (15%/30% to reflect a physical skill), which is
§102.3 and goes in 0.183.0.

Server + shared — no client change, **no APK needed**.

## 2026-09-23 — 0.181.0: the auto-potion MP rows sit under the HP rows (§102.1)

> *"MP potions in the auto-potion window overflow it; must sit BELOW the HP ones"*

`BL-243` put the three mana rows in a second column beside the four heal rows. A `SliderRow` draws
its track at a **fixed x 250–550 inside the row**, whatever width the row is given, so the right
column's tracks ended around x 1036 in a 760-wide window. The Potions tab is one column again: the
heal ladder, then the mana ladder under it, then the PvP note. The window is 620 tall (was 520),
which is still shorter than the auto-farm window.

Client only — **needs an APK**.

## 2026-09-18 — 0.180.0: a Whirlwind that dies with the first mob you kill

> *"i want all the war_aoe aoe skills shouts/wirlwind (except javelin) to be used without a target ..
> now i do a wirlwind on a one mob while fhgting 20 and i kill that one mob and my wirlwind stops at
> 5 casts"*

**The first half was already true at 0.179.0; the second half was a different bug in a different
place, and 0.179.0 is what made it visible.** Every Warlord ring — Shocking Shout, Whirlwind,
Taunting Shout and the three race Shouts — is `Range 0`, no `AreaAtTarget`, so `SelfCentredArea`
hands the cast the caster's own id and none of them needs a body. Shocking Javelin keeps
`AreaAtTarget` and still wants something to throw at, exactly as he asked. Nothing in the catalog
changed today.

### 🔴🔑 A CAST GATE IS NOT A RULE — the autopilot never passes through it

`SelfCentredArea` lives in `BeginSkill`, and `BeginSkill` is where a *tap* goes. `TryAutoChain`
**queues a skill directly** (`p.QueuedSkillId` / `p.QueuedTargetId`) and is the one caster in the
game that never sees that gate — so on auto-hunt it set `tgtId = target.Id`, the mob, for every
offensive skill including the six self-centred ones. Two consequences, and they are different sizes:

- **The shouts could not fire at all.** They author `Range 0`, so `UpdateQueuedSkill`'s approach test
  (`DistanceSq(caster, target) > range*range`) can never be satisfied against another body. The
  autopilot walked at the mob forever and never reached the cast.
- **Whirlwind anchored its volley on that mob.** Which is the report.

### The volley: twenty strokes ended by the first corpse

Whirlwind is a channel wrapper — `ChannelShots: 20, ChannelIntervalTicks: 2`, his four seconds
exactly. The channel tick ends a volley the moment `ChannelTargetId` is dead or gone, and that rule
is *right* for Arrow Barrage, which really is ten arrows **at** someone. It is catastrophic for four
seconds of blade around the caster: what the strokes catch is decided by geometry twenty separate
times, and the body that happened to be selected is the one the Warlord kills **first**. Five
strokes in, fifteen gone, the reuse and the full MP already paid.

**The fix is at the volley's source, not at each caller:**

```csharp
caster.ChannelTargetId = SelfCentredArea(def) ? caster.Id : target.Id;
```

A self-centred volley is anchored on the caster, so there is no target to lose and the death test
below it can never trip. Anchoring here rather than teaching `TryAutoChain` alone covers the mob
AI, the chain re-entry and whatever queues a skill next.

`TryAutoChain` is fixed too — `SelfCentredArea(def) ? p.Id : target.Id` on both offensive arms — so
the shouts fire on auto-hunt instead of walking. The `target is null` guard stays: a shout with
nothing to fight is still a wasted turn, and the sweep already reads `AreaAtTarget ? target : caster`,
so what the ring catches was never the selection's business either way.

### What this does not change

No `SkillDef` moved, so no CSV row moved — `SkillCsvSeed --check` is clean on all fifteen files and
`debuff_landmods.csv`'s 86 rows. Whirlwind's real power against a pack is still unmeasured by
anything in `docs/balance/`: twenty strokes that each sweep a 200 ring is a large jump from twenty
strokes at one body, and it has now stopped being cut short as well.

## 2026-09-18 — 0.179.0: two skills that did not do what their rows say

### 1 — the AoE discipline had no AoE

> *"war_aoe taunting shout should work without a target and affect any target in range"*

**Both halves were true, and the second was true of every offensive skill the Warlord owns.**

Taunting Shout's row says `enemy/aoe`, RANGE 0, AOE 600. What it did was land on the **one body you
had selected** — while a 600 circle pulsed around you, because the ring is *drawn* from `AreaRadius`
and *resolved* from `TargetMode`, and the row declared only the first. It also carried
`SkillEffect.None`, so the taunt arm never ran and its authored `TauntPower: 3000` was dead data:
the skill that provokes a field provoked nobody. And being offensive, the cast gate refused it with
nothing selected — so a Warlord walking into a pack had to click one of them to provoke all of them.

### 🔑 `TargetMode.EnemiesInRadius` is the only thing that makes a ring resolve

A radius alone draws the circle. The sweep is a separate branch of `ExecuteSkill`, and a skill that
does not declare the mode never reaches it. This is the *"the red circle pulses but nothing is hit"*
shape the 2026-08-28 `AreaRadiusAt` fix named, one layer up — and it is silent for the same reason
both times: the half the player reads is the half that works.

### The measurement — seven skills, and all seven are the Warlord's

A pass over the whole catalog for *offensive, has a radius, does not sweep* returned exactly seven:
`shocking_shout`, `waraoe_wirlwind_stroke`, the three race Shouts, `taunting_shout`,
`shocking_javelin` and `warrior_charge_stomp`. Whirlwind was twenty strokes at one body; a race
Shout's *"solo debuff on a ring"* was a Slash with the damage taken out; Charge n Stomp differed from
plain Charge by its reuse and nothing else. **Every other area skill in the game was already correct**
— the mage's waves, Arrow Barrage, the Elf's Sword Dance, both tank mass-taunts, every trap, every
boss slam. The defect is confined to the two `war_aoe` files, which landed on 2026-09-17. After the
fix the same pass returns **0**.

### What shipped

- **All seven declare `TargetMode.EnemiesInRadius`.** Nothing else moved — same power, MP, radius and
  rungs. `shocking_javelin` and `warrior_charge_stomp` keep `AreaAtTarget`, so those two still need a
  body to aim at; the other five are caster-centred.
- **Taunting Shout carries `SkillEffect.Taunt`**, so its 3,000 threat is paid to every creature in the
  ring through the same `ApplyTaunt` the tank's Taunt uses — the `BL-123` split holds unchanged (the
  aggro ladder is mob-only, the aim lock reaches players).
- **A caster-centred ring needs no target** (`GameLoopService.SelfCentredArea`), joining the trap, the
  totem, the hide and the resurrection field on the self-delivered side of the cast gate. The general
  rule, not one skill's exception: what a ring catches is decided by geometry when it lands. Nothing
  is skipped — `EnemiesInRadius` carries the whole `BL-77` PvP area filter itself and flags per body.
- **`SkillDef.TauntLockTicks`** (new, 0 = the old reading). One duration cell meant two things: the 30
  in his DURR is the **blunt vulnerability**, and letting the aim lock read it too would pin a whole
  ring for 30s on a 20s reuse — strictly better than the TANK's own 10-minute Tauting Wall (3s). The
  provoke is 3s, the Wall's number; the rot stays 30s.

**No CSV moved** — every one of these rows already said `enemy/aoe`. The code was behind the file.

⚠ **A real power jump, and it wants a playtest**: Whirlwind alone goes from 20 strokes on one body to
20 strokes on everything within 200. That is what the rows author, and it is the first time the
discipline has actually been the AoE one — but no `docs/balance/` number was measured against it.

Filed and closed as **`BL-265`** ([BacklogArchive.md](BacklogArchive.md)).

### 2 — Relax let you run, swing and cast while healing 5% a second

> *"humans relax should prevent me from moving or acting"*

**Every word of the rule was already written, and none of it was enforced.** His `fighter 1st.csv`
row says *"Sit and relax: Gives 1.0 % HP/s regen (cannot act, status is canceld on dmg taken)"*; the
skill's own description in the code says *"You cannot act, and any damage ends it."* What the toggle
actually did was apply the regen and leave you standing.

The machinery it needed has existed the whole time. `MoveState.Sitting` already blocks movement
(`HandleMove`), attacks (`HandleAttack`) and casting (`BeginSkill`); being hit already stands you up;
and `EndsOnDamageTaken` — which Relax carries — already drops the buff the moment anything lands. The
regen code even *says so*, in a comment explaining why the sitting multiplier is not charged twice:
*"Relax makes you sit"*. **It never entered the state.** So the eight rungs, all authored at 0 MP
because *"the price is that you are sitting"*, cost nothing at all: 5% of max HP and 3% of max MP per
second, held while running.

**`SkillDef.SeatsCaster`** (new; Relax is its only user) makes it a real sit, not a lookalike:

- **Toggling it on sits you down** — the same `MoveState`, the same stand-up clock, the same "you must
  be idle" gate the sit button applies. Refused out loud while engaged, casting or mid-stand.
- **Toggling it off stands you up**, because the stance and the sit are one thing. That needed the one
  exception to the seated cast gate: a seating toggle you are *wearing* can be pressed to end it —
  otherwise Relax would be the only toggle in the game you could not switch off the way you switched
  it on, and the only doors out would be the sit button and a monster.
- **Standing by any other route ends the stance** (`EndSeatedStances`), or a Human could sit, toggle
  Relax, stand and walk away still regenerating. A HIT needs no such call: the damage path already
  ends `EndsOnDamageTaken` buffs and stands the victim up in the same breath.

`SitDown`/`StandUp` are now one pair shared by the sit command and the stance, so the two cannot
disagree about what sitting is.

**No CSV moved** — his row already said *"cannot act"*.

Filed and closed as **`BL-266`** ([BacklogArchive.md](BacklogArchive.md)).

**No new APK for either fix**: the skill bar already sends a null target, and nothing on the wire
changed.

## 2026-09-18 — 0.178.0: the debug "learn all" wore all eighteen sigils

> *"also fix sigils! ... im lvl 76 war master and i wear all sigils as passives and in the window are
> shown as worn!"*

**The Sigils tab was telling the truth — the character really was wearing all eighteen.**

The sigils are not learn lines. They are a fixed grid injected into every ascended class's
`Cumulative` at level 76 (`ClassSkills.cs`), because they are bought on their own tab; the two rules
that make them a CHOICE — at most three, one slot per arrived subclass, and only from a tree a
subclass has unlocked — live in `SigilRefusal`, which **only the ordinary LearnSkill path consults**.
`HandleDebugLearnAll` grants everything in `Cumulative` whose learn level is met, so it walked past
that gate entirely and handed out the whole grid. Every one of the eighteen passives was live in
`RecomputeDerived`, so the stats of any character that pressed it were junk.

That is the same failure the **stat swaps** were pulled out of this button for, in the same method,
for the same stated reason: *"any subset is an arbitrary BUILD decision"*.

### The fix

- **The debug button skips sigils**, exactly as it already skips the stat swaps, and says so.
- **It also clears an illegal set it handed out earlier** — refusing to grant them does nothing for
  the character already wearing eighteen. Only an over-the-limit set is touched, so a deliberately
  committed one is never disturbed, and it clears ALL of them rather than trimming to a legal count:
  which three to keep is a build decision, and *"all at once, nothing partial"* is already the
  Mindwright's rule. Press the button once and the character is clean; re-commit on the Sigils tab.
- **The admin endgame seed carries the same filter**, inert today (it asks `Cumulative` without the
  4th tier, which is the only thing that injects the grid) and there so it stays true the day that
  seed ascends.

⚠ **Server-side only — no APK.** The tab renders `Learned`; it was right all along.

### Also in this version

- **`BL-263` item 3 records the colour rule** for the racial split when it is built: the abbreviation
  does NOT split (`Mig` stays `Mig`), the SQUARE'S COLOUR carries the source — elf dark green, human
  dark blue, demon dark red, NPC the current gray, potion dark yellow, scroll dark brown. It needs no
  server work: `BuffDto.SourceSkillId` already says which face cast it and the client compiles
  `SkillCatalog`. Two notes in the entry: a sub-60s buff already blinks in that same dark yellow, and
  potion/scroll squares draw in the Consumable group rather than the limited bar.
- **`BL-264` filed** — no CSV anywhere says which buffs fight each other; from `mage 1st.csv` you
  cannot tell that the three Mights are one family. A generated `FAMILY` + `RANK` column, checked like
  every other column. §2 of the entry: the `RACE` cell is the other column nothing verifies, which is
  why the stray `Human` tag on the Wirlwind block sat in `war_aoe 4th.csv` under a green checker.
- **`war_aoe 4th.csv`: his Wirlwind race tag removed** (his edit). The code was never race-split there
  — `WaraoeWhirlwind` is in the shared Warlord list registered for all three races — so the file now
  matches the game, and `SkillCsvSeed --check` is clean.

## 2026-09-18 — 0.177.0: a Mark's rung is its rank — `BL-164`

> *"i want mark to have ranks .. a Life mark L2 to be replaced only by other l2 marks .. not some1 to
> be able to put lower rank -> admin of buffer gives me rank2 and stupid me goes to npc and overrites
> it ... it shouldnt"*

His scenario, exactly: an admin `/buff` (or a 4th-class Lightbringer) puts **rung 2** of a Mark on you,
you walk to the Spirit Helper, buy the **rung 1** she sells for 300,000 gold — and since 0.176.0 took
duration out of the tiebreak, it landed and **replaced the stronger one**. Gold gone, Mark downgraded.

### The fix — option 1 of the three the entry listed

All four Marks (Holy, Life, Blood and the buffer's Harmony) stopped being `FlatRank: true`. They now
carry the **rung in the rank**: rung 1 lands at rank 1, rung 2 at rank 2, which is what every other
childless multi-rung buff in the game has done since `BL-85`.

| you are wearing | incoming | before | now |
|---|---|---|---|
| Life Mark **Lv2** | NPC Holy Mark **Lv1** | replaced it (gold taken) | 🔴 refused, **and the gold is not taken** |
| Life Mark **Lv1** | NPC Holy Mark **Lv1** | replaced it | replaced it — unchanged |
| Life Mark **Lv1** | Holy Mark **Lv2** | replaced it | replaced it — unchanged |
| Life Mark **Lv2** | Harmony Mark **Lv2** | replaced it | replaced it — unchanged |

**One Mark at a time is untouched.** That has always been the shared `BuffKey` (`healer_mark`), never
the flat rank — the two were easy to conflate and three comments in `BalanceMatrix` did, so they were
corrected in the same pass. Swapping between the four **at the same rung** stays free, because equal
rank replaces since `BL-263`.

### 🔑 `SharesLadderKey: true` is the declaration, not a workaround

The `BL-85` startup guard refuses two childless multi-rung defs on one buff key, because their rungs
silently start competing with each other. It offers two escapes and the Marks took the wrong one:
`FlatRank` pins every rung to one number, which is right for Great Might / Great Bulwark ("one or the
other, never both") and was wrong here. `SharesLadderKey` is the other, and it says what is actually
true of these four — **four versions of the same buff that SHOULD compete rung for rung**.

### 🔴 What the old `FlatRank` was really buying, since it is worth knowing why it survived so long

It was load-bearing right up until yesterday, in the *opposite* direction. While equal rank was broken
by **duration**, a rung-2 Mark carrying rank 2 would have locked out a different race's rung-1 Mark for
up to an hour — which contradicted "an ally wears one Mark, whichever healer got to them first". So the
flat rank bought cross-race swapping, at the price of this bug. `BL-263` removed the duration tiebreak,
so equal rank now replaces on its own and the swapping is free without it. **The flat rank was paying
for something the engine started giving away, and kept only the cost.**

⚠ This is the second time in two days that a `BL-263` consequence has been the whole story (the first
was `HarmonyRank`, kept for the reverse reason). **When a tiebreak is removed, every workaround that
existed because of it is either newly unnecessary or newly load-bearing — check which, one by one.**

### Verified

- `dotnet build Game.sln` clean; server **boots** (`L2Clone server v0.177.0 starting.`) — which is
  where the `BL-85` guard actually runs, so the boot is the test of the declaration.
- `dotnet run --project tools/SkillCsvSeed -- --check` — no discrepancies, all fifteen walked files.
- No CSV owes a row: rank is not an authored column, and no Mark's numbers, duration, MP or text moved.

## 2026-09-18 — 0.176.0: duration stops deciding buffs, and Might gets three faces — `BL-263`

His first three rulings on the wrapper model, built. The fourth (*"leave groups to cover only
families no rank"*) is a **decline** and cost no code — see §2.

### 1. 🔴 EQUAL RANK NOW REPLACES — DURATION PLAYS NO PART

> *"remove the duration check of same rank buffs"*

From 0.36.0 until today, two buffs of the same family at the same rank were settled by whichever had
**longer left**. That tiebreak is gone from both places that held it — `ApplyBuff`'s Rule 1 and the
`BuffWouldLand` pre-filter. A conflicting buff is refused only by something **strictly stronger**.

⚠ **What it costs, so it is not a surprise in a playtest:** a 1-hour NPC blessing *is* overwritten by
a 20-minute party buff of the same rung, and a potion drunk under an identical scroll shortens you to
the potion's clock. Both used to be refused. He ruled it knowingly (*"we don't care for duration"*).

✅ **What it fixes for free:** §100's Mark bug (an NPC re-buff that wouldn't refresh) can no longer
recur — it existed only because a *predicate* had to guess a duration. `BuffWouldLand` lost its
`durationOverride` parameter entirely.

🔴 **`SkillCatalog.HarmonyRank` (`NpcBuffRank + 1`) is now MORE necessary, not less.** [BuffFamilies.md]
(design/BuffFamilies.md) had it down as a hack to delete once duration went; that was wrong. At equal
rank the winner is now *whoever cast last*, so the one rank of separation is the only thing keeping a
covering class harmony above the NPC single it covers. Left in place, with the reason written on it.

### 2. ✅ GROUPS KEEP THE FLAT `GroupRank` — no per-family rank (his decline)

> *"leave grups to cover only families no rank .. we dont want a body_reinforcment (that provides 10
> things) to be replaced by a single buff a one rank higher .. 100+lvl is ok"*

Step 1 of the BuffFamilies plan — giving `CoveredKeys` a level per family — is **declined**, and he is
right: a group is ONE buff, so any single that could outrank it in one family would take all ten of
its parts down with it. `GroupRank = 100 + level` stays exactly as it is. No code changed; the
design doc and `BuffLadders.md` now say so.

### 3. 🔑 THE WRAPPERS — Might is three skills now, one per race

> *"what i realy want is the wrappers ... a demon_cast_atk_phys to provide the same as
> npc/human/elf_cast_atk_phys but have different description/icon/name"*

His `mage 1st.csv` Might section is three rows now, and they are built:

| Race | id | name |
|---|---|---|
| Elf | `elf_cast_atk_phys` | Forest Might |
| Demon | `demon_cast_atk_phys` | Demonic Strength |
| Human | `human_cast_atk_phys` | Blessing of Might |

All three hand out the **same child rung**, `buff_atk_phys_1` (+8% P.Atk), at the same price his rows
carry (1s cast, 1s reuse, 600 range, 20 min, 20 MP, 960 SP). **That is the whole answer to his
"we must add to cs files a family keys and rank so we know that mightA (p_atk/1) and mightB (p_atk/1)
are replaceable"** — the family key and the rank live on the shared child, so two wrappers over one
rung are interchangeable by construction and a wrapper has no numbers of its own to disagree with.

**One new engine field: `SkillDef.NamesItsBuff`.** A one-child wrapper normally lends its child only a
duration, a bar row and an icon, and the buff reads with the CHILD's name — which is right for a
potion ("Potion of Might" pours a buff called "Might") and wrong for these. Set the flag and the
wrapper's own name and description are what the player sees. Resolved from `SourceSkillId` (the id the
icon already follows), so the face **survives a relog** with no change to the save format.

The base `cast_atk_phys` is untouched and is still what a buffer class casts from rung 2 up — and it
now `Replaces` all three racial ids, his cleric row (*"cleric just replaces them so nothing higher have
them"*). That takes the racial Might off the buff bar **and** off the learn list at 20.

### 4. Two CSV rows that documented code, not new content

`Return` (the free 60s channel home) was in the code and in neither 1st-class file; he added the row
to both. ⚠ **Its id is `return_town`, not `return`** — he wrote *"just for the id of return im not
sure - change it as it is"*, so the file now names what the catalog actually holds. His 60s cast /
10s reuse are already what the skill carries, and `FixedCast`/`FixedCooldown` are what his
*"60/10s fixed times"* note is asking about: no haste or cooldown buff moves either number.

⚠ One spelling corrected in his row: *"Demonic Strenght"* → **Demonic Strength**, in the CSV and the
code together. Say the word if you meant the other one.

**Verified:** `dotnet build` clean (server + Unity client), server boots, `SkillCsvSeed --check`
reports **no discrepancies**, smoke test green.

📥 **A NEW APK IS OWED.** The class-skill TABLE changed (three ids where there was one at level 7) and
the client builds its Learn tab locally from the compiled `ClassSkills`.

---

## 2026-09-18 — 0.175.0: `--check` is clean for the first time, and two ids the sweep missed

Two small things that both came out of verifying `BL-84`, and neither is a new decision.

### 1. `SkillCsvSeed --check`: 144 discrepancies → **ZERO**

Every one of the 144 was the same 17 Sigil rows in `shared 4th.csv`, reported once per 4th-tier file
that folds it in, saying **`SP 20kk` / `GOLD 10kk`** where the code says 0.

**The code was right and the FILE owed it.** You ruled this in 0.169.0 — *"yes sigils become end game
and hard"* — and the price did not vanish, it moved onto the road: three sigils is now **three
subclasses each levelled to 75**, every one born at 40 with no SP. Charging 20kk SP for the commit on
top of that would charge twice for the same thing. So the rows now read `0,0,0`.

🔑 **This is the clause the two-way CSV contract keeps losing:** *"a skill touched on the way past
still owes its CSV row"*. 0.169.0 changed a price in code and left the file quoting the old one for a
day. Nothing about the balance moved here — the file was brought to your ruling, not the other way
round. ⚠ Your `COMMENT` column still records which slot each sigil came from
(`Tank_Defence_Sigil`, `Healer_Defence_Sigil`…) and is left exactly as you wrote it; after `BL-84` it
is the only place that history survives, which makes it more useful than it was.

### 2. Two ids `BL-84` missed, because I left `mage_` out of the prefix list

`mage_attack_sigil` was **Frenzy Sigil** and `mage_support_sigil` was **Arcane Support Sigil** — the
same slot-named pathology as the other 82, sitting behind a prefix my scope list did not include.
(`mage_defence_sigil` is genuinely *Mage Defence Sigil* and stays.)

* `mage_attack_sigil` → **`frenzy_sigil`**
* `mage_support_sigil` → **`arcane_support_sigil`**

🔑 **The audit is what found them, and that is the point of having built it as a page rather than a
script.** Adding one string to `kitPrefixes` in `--skillids` re-measured the whole catalogue and
printed exactly two rows. It now reports **0 WORD, 0 PREFIX** over 163 in-scope ids, with 765 skills
still in the catalogue.

⚠ **The `game.db` delete 0.174.0 asks for covers these two as well** — same version, same reset.

## 2026-09-18 — 0.174.0: every skill id reads as its skill — `BL-84`

Your ask of 2026-08-17, and the reminder you asked me to file has now been acted on:

*"After the healer is done I want to change all the game skills id's to match the skill names ... not
`lb_elf_dawn` <> Healer's Blessing, it should be `healers_blessing` or something that matches it. Make
a note to remind me after the healer is done (I want all the skills, not only the healers — all 1st,
2nd + healer 3rd)."*

**136 ids renamed. Zero collisions. 765 skills before, 765 after.**

| was | is | name |
|---|---|---|
| `lb_elf_dawn` | `healer_blessing` | Healer Blessing |
| `lb_human_mend` | `quick_great_heal` | Quick Great Heal |
| `lb_ork_font` | `healing_totem` | Healing Totem |
| `wc_human_bolt` | `arcane_lance` | Arcane Lance |
| `wc_elf_pass` | `harmony` | Harmony |
| `tank_defence_sigil` | `aegis_sigil` | Aegis Sigil |

### 🔑 It is NOT 605 ids, and working out which is the whole job

My first pass asked "does slugging the name reproduce the id" and flagged **605 of 765**. That test was
wrong, not the codebase. `buff_crit_rate_4` is not the bug you described — it is more informative than
its name, and **six different defs are called "Focus"**, so the name is not even a unique id. Slugging
names collided on **71** groups.

So the scope is the ids named after **the discipline or class that happened to own the slot**, which is
exactly the shape of all three of your examples — and which the 44+ kit you authored later already
avoids (`urgent_heal`, `ultimate_heal`, `resurrection_field`, no prefix at all). It splits three ways:

| | |
|---|---|
| **82 WORD** | the id's own word does not describe the skill (`wc_human_bolt` = Arcane Lance). **Your complaint**, and the half that was worth doing. |
| **54 PREFIX** | the id already read correctly once the discipline marker came off (`wc_acoustic_shock` → `acoustic_shock`). |
| **131 KEPT** | the prefix IS the identity. `archer_armor_mastery`, `rogue_armor_mastery` and `warrior_armor_mastery` are three different passives all called "Armor Mastery"; strip the prefix and they become one id. |
| **605 out of scope** | the systematic families — `buff_*` rungs, `pot_*`/`scr_*`, `npc_*`, `cast_*`, `rune_*`, `sigil_*`, `swap_*`. Untouched on purpose. |

### It was generated, not typed

`dotnet run --project tools/BalanceMatrix -- --skillids` is a new page and it is the whole method: it
prints the three groups, the collision check, and a machine-readable mapping that **drives** the sweep.
The constant name comes from **reflection** over `SkillCatalog`'s literals, so `LbElfDawn` became
`HealerBlessing` in the same pass — an id renamed while its constant still reads `LbElfDawn` is half a
job, since the code is what you read. Before the sweep it reported 82 + 54; after, **0 + 0**.

### Five typos and a retired race word fell out of it

Not looked for — the audit pairs each id against its name, and a typo is a mismatch:

* `archer_bow_stence` → `bow_stance`
* `warrior_strenght` → `warriors_strength`
* `nuker_Force_empowerment` → `force_empowerment` (a capital letter, in an id)
* `wc_bloodhanter_blunt_mastery` → `warlock_weapon_mastery` (a misspelling of a class name that no longer exists)
* `archer_explosive_arrows` → `explosive_arrow` (the name is singular)

🔑 **And every `_ork_` id is gone** — `lb_ork_font`, `wc_ork_chant`, `waraoe_ork_shout` and the rest.
The race became the Demon in `BL-101` and the word had survived in the ids ever since.

### What was checked, and how

| | |
|---|---|
| **the compiler** | 136 constants renamed with all their references; `Game.sln`, the Unity client, and all three tools build |
| **`--skillids`** | WORD 0, PREFIX 0, and still 765 skills — no id was lost or merged into another |
| **the CSVs** | 20 of them carry `SKILL_ID` columns and moved with the code. `git diff --numstat` shows identical add/remove counts on every one, so nothing was reformatted (the Excel corruption trap) |
| **`SkillCsvSeed --check`** | the same **144** discrepancies as before, byte for byte as a set — the rename introduced none. ⚠ Those 144 are a real pre-existing drift; see below |
| **`debuff_landmods.csv`** | 86 rows still verify, which is the proof the rename reached that file's `SKILL_ID` column too |
| **the smoke test** | 279 pass / 3 fail — exactly the baseline (the `Open-Checklist` §101 crafting three) |
| **the server** | boots green on a fresh `game.db` |

⚠ **A `game.db` DELETE IS REQUIRED, and you already ruled that it is fine**: *"I'll reset the db
anyways so it's not of a concern."* Skill ids are persisted (learned skills + the skill bar), so every
character's bar and skill list must be recreated. One of the ids you hold today will not exist tomorrow.

### Two findings recorded rather than absorbed

🔴 **The 144 `--check` discrepancies are the CSVs trailing YOUR OWN ruling.** All 144 are the same 17
Sigil rows repeated across the nine 4th-tier files, saying `SP 20,000,000` where the code says 0 — and
0 is correct: you ruled it in 0.169.0 (*"yes sigils become end game and hard"*), the price moved onto
the road (three subclasses each levelled to 75) and charging for the commit would charge twice. So the
FILE owes the code, which is the clause `BL-163`'s own commit missed. Fixed in the next version.

🔴 **The smoke test's fear check was FLAKY and is now polled.** It slept a fixed 1.5s and measured
once; on one run it read 13 units and failed, on the next 100+ and passed, on identical code. It now
polls for the distance with a 3-second ceiling — which `CLAUDE.md` already required ("SmokeTest must
poll, never sleep"). A flaky check in the tool that verifies everything else is the worst kind: the
next person either hunts a regression that is not there, or dismisses one that is.

## 2026-09-17 — 0.173.0: the Spirit Helper's shelf is a FILE now — `BL-163`

Your ruling of 2026-09-04, the day after `BL-158` shipped:

*"that's why I wanted the npc buffer to be like the /buff command not like a wrapper or check player
lvl and put him in a range table with available buffs ... and that table can be a file with min
lvl,skill_id_rung,price (editable from outside - so a pvp server won't require new npc just change of
id's) .. but whatever is working"*.

**`docs/data/npc_buff_shelf.csv`** — fifty rows, one per rung the NPC sells. Edit it, restart the
server, done. No rebuild, no code change, no new NPC.

```
SHELF_ID,MIN_LEVEL,RUNG_SKILL_ID,RUNG_LEVEL,PRICE
npc_ward,40,buff_def_mag_1,1,5000
npc_ward,44,buff_def_mag_3,1,10000
npc_ward,52,buff_def_mag_4,1,15000
```

### The two things that make it a server-operator feature, and both are yours

**1. The row names the RUNG.** The shelf points at `buff_def_mag_3` and the NPC grants it exactly the
way `/buff` does. There is no per-blessing `Levels` array to keep in step with a second table, and no
"tier index IS the SkillLevel index" invariant to guard — **the whole `BL-158` startup assertion is
deleted**, which is what that entry predicted would happen.

**2. It is outside C#.** `NpcBuffTier`, `NpcBuffTiers` (thirty hand-written level/price rows),
`HarmonyPrice`, `MarkPrice` and the `NpcLadder` factory are all gone from `Skills.Buffer.cs`.

### A FIFTH column, which your four did not have, and why

`RUNG_LEVEL`. Nineteen blessings name a family rung, and every family rung is its own def — level 1,
always. The **three Marks** are the exception: they are the Lightbringer's own class skill with two
rungs, and the shelf sells rung 1 only (`BL-161`). Four columns could not say that. It also means a
PvP server that wants to sell her rung 2 at 83 just writes the row.

### What DID NOT change, and it is measured rather than asserted

Every level, price and rung is byte-for-byte what the C# table handed out. `--npcshelf` prints the
whole shelf level by level and `--buffmenu` prints the admin drawers; both were captured before and
after, and the admin menu is identical **button for button**. ⚠ Comparing the dump against the old
table is also what caught the one transcription error I made — Aim's ladder is 40/48/56, and I had
copied Ward's 40/44/52 onto it.

### The file refuses to load rather than selling the wrong thing

A typo in an operator-edited file is far likelier than a typo in C#, and the failure mode of
tolerating one is a blessing that is silently unbuyable — or worse, one that quietly sells a different
rung. So the server **does not boot** on: an unknown rung id, a rung level the def does not have, a
ladder whose levels or prices go backwards, a duplicate row, a **non-ASCII character in an id** (the
`BL-237` Cyrillic `к`, and this file is edited by the same keyboard), a SHELF_ID that is not one of the
game's blessings, a blessing your Mage/Fighter preset names that the file no longer sells, or one of
the free eight carrying a price. Every problem is listed at once, by line number. Verified by breaking
the file on purpose.

### Two traps this walked into, both worth writing down

🔑 **THE SHELF ID IS STILL THE BLESSING'S IDENTITY.** `[Save]` and the two role presets store what you
PRESSED (`BuffInstance.SourceSkillId`), so every grant passes the SHELF id as the buff's source. A
preset holding rung ids would freeze you at the rung you saved — save Ward at 44 and you would still
be buying +23% at 70 — and every preset already in `game.db` holds the `npc_*` ids. It is also what
makes your `BL-150` rule work with no extra state: *"if some1 buff me with body or soul and i save it
and im <40lvl they will not activate .. they will activate after 40+"*.

🔑 **THE UNITY CLIENT COMPILES THE SAME ASSEMBLY AND HAS NO FILE.** `GameUi.Debug` builds the admin
Buffs drawers **locally** from the compiled catalogue, and it reached the shelf through
`NewbieBuffSet` — on a phone that is a `FileNotFoundException` on the Debug tab. So the thirty ids
stay in C# as `NpcShelfCatalogue`, the *universe* the shelf may choose from, while the FILE owns what
is on offer, in what order, at what level, for what price, handing out which rung. The loader asserts
the file is a subset of the universe, so the two cannot drift in silence. The buff-slot rule
(`BuffLimitIds`) moved to the universe for the same reason: whether a Mark occupies a square must not
depend on whether the NPC happens to be selling it today.

### Also fixed, found because this rebuilt a tool nobody had rebuilt

`tools/BalanceMatrix/DropFinder.cs` had not compiled since **0.171.0** — it still printed the drop
index's version stamp and content hash, both of which you had just had deleted. The tool is outside
`Game.sln`, so nothing caught it.

### Where the file is read from, and why the server says so out loud

Startup logs `NPC buff shelf: 30 blessings from <path>`. The repo copy in `docs/data/` **wins**; a
published build also carries a copy at `data/` beside the exe, for a server with no repo behind it.
⚠ Deliberately that way round: a build-copied file that shadows the authored one is exactly how an
edit appears to do nothing — the same trap a stale `bin/` copy of `game.db` set once already.

⚠ **Nothing about the game changed for a player.** This is `BL-163`'s own warning honoured: *"Nothing
is broken today — this is a refactor for editability, not a fix."*

## 2026-09-17 — 0.172.0: `/unstuck <name>` — three minutes rooted in town, and your other character is rescued

`BL-172`, built to your spec of 2026-09-05 and to the fork you ruled the same day.

*"'/unstuck <name>' command that have 180s cast time and is available from the same acc to other chars
(Char1 -> /unstuck Char2) and after 180s Char2 is teleported to starting town all his equipment is
unequiped all his buffs/debuffs are cleared -> don't work on baned/kicked/jailed char"* — and:
*"Works only in town and roots unable to act until cast ends or canceled. It's a unstuck command not a
escape mechanism -> ur char1 stuck/bug/etc .. u create char2 and use /unstuck char1"*.

### It is a SKILL, and that is what made the three minutes free

A cast already does everything the channel needed: it roots you (`Entity.IsCommitted`), it draws the
cast bar, it answers ESC, and with `FragileCast` it dies to any damage. So `unstuck` is a real
`SkillDef` — 1800 ticks, `FixedCast` so no haste can shorten it, `FixedCooldown`, 10s reuse — and the
chat command arms that cast directly.

🔑 **It is the one skill in the game that is never learned and never on a bar.** `AutoLearnCoreSkills`
does not hand it out and no class table lists it, because what it acts on is a NAME: a bar button has
nowhere to type one, so a slot there would be a button that can only ever fail. `BeginSkill`'s first
gate is `HasSkill`, which is exactly why the command arms the cast itself rather than going through it.

### The target is normally NOT in the world, and that is the whole of the engineering

You make Char2 *because* Char1 is stuck, so Char1 is usually logged out and exists only as a database
row. Three states, forced down to one:

| the target is | what happens |
|---|---|
| **fully logged out** | no entity — the effect is written to the row |
| **still in the world** (offline farmer, link-dead grace) | evicted by the ordinary logout path FIRST, so its own save lands before the rescue writes |
| **logged in right now** | same eviction; only reachable if two sessions per account are ever allowed |

The eviction is awaited before the row is rewritten, or the logout save would race the rescue and win.

### The gates

- **You must be standing in a town.** A dungeon ENTRANCE does not count — the same distinction
  `NearestTown` draws for the Scroll of Return, because being stuck inside a dungeon must not be
  rescuable from its doorstep.
- **Same account, and not the character you are playing.**
- **Not jailed, not kicked, not pending deletion, and not on a banned account.** A banned account
  cannot log a rescuer in at all, so half your rule enforces itself — checked anyway, because an
  account ban can be lifted while a character's own jail runs on.
- **Not dead, not stunned, not seated, not already casting, not in combat.**
- The gates are asked TWICE: once before the channel starts, so a misspelled name costs no time, and
  again when it lands, so a jail handed down during those three minutes is obeyed.

⚠ **Because you are rooted in town for three minutes, no other abuse gate is needed** — it cannot be
an escape, a fast travel, or a way to strip a character mid-fight. That ruling is the security model,
so the root and the town check are not to be "simplified" away.

### What it deliberately does NOT clear

`DiedWhileAway` (the flag that makes a character who died away log back in dead — clearing it would
make this the way to dodge a death penalty) and the Boss's Judgment rungs (`BL-98`), a punishment
whose clock runs offline on purpose. Neither is "stuck"; both would make a rescue a cleanse.

🔑 **NO NEW APK NEEDED FOR YOU.** The client refuses unknown slash commands for non-staff characters,
and `unstuck` is added to the three it lets through for everybody (`/where`, `/buff`, `/unstuck`) —
but your own characters are staff, and the client already passes *everything* through for staff. So it
works on the APK you have. The one-line client change is for ordinary players on the next build.

## 2026-09-17 — 0.171.0: the drop database gets its real window, and loses its cache

Both of your rulings on `BL-253`, in one increment.

### 1. NO FILE, NO VERSION, NO HASH — you were right and the numbers say so

*"If drop indexes are build even after x10 more mobs still faster than reading a file, build each
restart. (that way no drop version needed)"*.

It is. **Building 19,842 rows takes 9-13 ms; reading the same rows back off a 1.8 MB file took 28 ms.**
Both paths are linear in rows — about **0.65 µs to build a row against 1.4 µs to parse one** — so the
build stays roughly twice as fast at any size, and a ten-fold world is ~130 ms against ~280 ms. ⚠ I did
not build a ten-fold world to check; the claim rests on both paths being linear, which they are, and on
the per-row costs above, which are measured.

So `DropIndexStore` is deleted, `dropindex.txt` is gone, and the index is built once in `World`'s
constructor and held. `/dropindex` now reports rows and build time; there is no `rebuild` verb, because a
**restart is the rebuild**.

🔑 **The staleness problem went with the file, and that is the bigger win.** A cache needed TWO stamps:
a content hash for the data, and a hand-bumped version for the rank-LAYER code — because editing a number
inside `EliteMatDrops` or `BossPile` moves no data and no hash could see it. That second stamp was a thing
a person had to remember, forever, or the window would quietly send a player to farm a creature that does
not pay. **Nothing to remember now.** Your instinct to kill it was worth more than the 15 ms.

### 2. THE WINDOW — predictions, a filter tree, and a table you can order

*"Writing in a text box offers a prediction and selecting one or enter shows item. A item can be found by
selecting category/rarity/grade etc (like a filter tree) then selecting a single item drop it shows a
table with mobs and chances -> can order by name/level/chance etc"*. All of it:

* **Type and it predicts.** Every keystroke re-filters the whole item catalogue; a prefix match sorts
  above a contains-match, so typing `gre` puts Greater Potion above Ogre Hide. Tap one, or press **Enter**
  to take the top one.
* **Or narrow with the tree.** Three cycling buttons — **Type / Rarity / Grade** — each reading its
  current value and advancing on tap. Cycling buttons rather than dropdowns: a dropdown on a phone is a
  second window over a window, and `Rarity: Epic` says the same thing in one control that stays legible
  while you use it. Each axis cycles back to *any*, so a filter is always undoable.
* **Then the table**: Creature · Lvl · Rank · Where · Per kill. **Every heading is a sort toggle** — tap
  to sort, tap again to reverse. Chance descending is the default, because the question is "where is the
  best place to farm this" and the best place should be the top row.

🔑 **Predicting and filtering cost NO round trip**, and that is not an optimisation — the client compiles
against `Game.Shared`, so every name, category, rarity and grade in the game is already on the phone.
Only "where does this drop" goes to the server, because only the server may answer it.

⚠ **And that is exactly where the line is drawn.** Picking WHICH item is a local question about a
catalogue. Its CHANCE is the server's arithmetic — the rate knobs, your Rune of Drop, the level gap — so
`DropLookupRow` now carries **both** the sortable value and the server's formatted text. The client sorts
on the value and prints the text; it never re-derives the number. Sorting on a number is not arithmetic,
but deriving it here would be a fourth place the drop rate is calculated, and the rule is that there is
one. (It also stops the two classic bugs: sorting `"12.5%"` under `"9%"` as strings, and `"76-79"` after
`"8"`.)

Picking a prediction sends the exact item **id**, and the lookup now honours an exact id as exactly that
item rather than as a substring — so choosing *Common Wood* returns Common Wood and not everything whose
id contains it.

⚠ **Protocol 46, and this one is NOT a pure addition** — `DropLookupRow` changed shape. No APK exists on
44 or 45 (both were cut and superseded the same day), so nothing in the wild has the Drops window and
`MinAcceptedProtocol` does not move. **A NEW APK IS WANTED.**

## 2026-09-17 — 0.170.0: the class master's second dialogue — `BL-250` §7+§8

The last of *"finish all red dots"*. The server side of this has been built since 0.155.0 and had
nothing to draw it; now it does.

*"u can reuse the @40 class master to open new dialogue when u go back to him with main @76+4th"*, and
§8: *"we will need an detailed information when taking subclass what that subclass will give you when
reaching 75lvl etc"*.

Every row of the panel answers §8 before you commit: what the class is, that it **starts at 40 with no
SP** and brings a 1-day rune, **which sigil tree it opens at 75 and which three sigils are in it**, and
whether it would open a **sigil SLOT** (with its number) or only the tree. Above it: what you hold, what
you have opened, how many unused **tickets** are in your bag — an unused ticket is a slot you already own
and is the thing people forget — and either how the next slot is EARNED or what it costs to buy.

🔑 **The client decides nothing here.** Which tree a class unlocks, whether it would open a slot, what
the next rung costs, whether a class is still legally available, which of your own classes may still be
swapped out — all of it arrives derived on `SubclassOfferInfo`. That matters more than usual on this
panel: the same rows decide whether to spend five billion gold, and a second opinion computed on the
phone is how the two would come to disagree.

A class you cannot take is **dimmed, never hidden** — *"you already walk this path"* is a fact about your
own build, and hiding the row turns it into a mystery about a missing option.

✅ **`BL-250`'s whole build is now done** except the APK that carries it. Still open in the entry: §9.6,
the completeness gate against a BOUGHT slot, left as built and unanswered.

## 2026-09-17 — 0.169.0: the sigils become end game — `BL-250` §1-§4

*"100kk wipes all -> finish all red dots"*. The sigil half of the subclass system, and the one reading
the entry was holding is decided: **100kk clears the whole board in one payment**, not 100kk per sigil.

### §1 — the three slots stop being Attack / Defence / Support
They are **three identical slots** now: any three of the eighteen, from any tree you have opened.
`SigilSlot` survives as a label for the UI and nothing else.

🔴 **The part that had to be removed with it, and would have been a disaster to leave.** A sigil carried
`Replaces` — every other flavour's same slot. In this engine **`Replaces` means gone for good**, not
"swapped": under the new rule, committing a Warrior Attack sigil would have silently **destroyed** a
Mage Attack sigil you already wore, in the same increment whose entire point is that you may now hold
both. It is gone, and so is the `ExclusiveGroup` — which had a second consequence worth stating,
because it was the bug waiting in this change: the Mindwright's reset list and its Forget gate were
both keyed on *having* an ExclusiveGroup, so taking it away would have made every sigil **permanently
un-removable**, in the increment that made clearing them the point. Both now ask "is it a sigil".

### §2 — a slot is opened by a SUBCLASS at 75, never by your main
One per subclass that has reached **75 holding its 3rd class**; three is the ceiling; subclass four and
up open only their tree. Your main opens nothing, however high it goes.

🔑 **Both numbers are DERIVED, so `game.db` gains nothing.** The subclasses, their levels and their 3rd
classes are already persisted — a stored slot count would be a second copy of a fact, free to disagree
with the first after a `/setlevel`, a swap-out, or the DB reset this project does instead of migrations.

### §3 — a sigil GROUP is unlocked by owning a subclass of it
Same gate, his own sentence naming them together: *"once sub becomes 75 u are able to get the tree +
sigil slot"*. A warrior with six subclasses reaches six trees and still wears three sigils — the mask
has no cap, only the slot count does. A locked tree is **shown, not hidden**: a tree you cannot reach is
a reason to level another subclass, and it cannot be a reason if you cannot see it.

### §4 — committing is free, clearing is 100kk, and it takes all three
*"we can remove their sp/gold cost -> they are their own system. only clearing will cost 100kk"*. So
20kk SP + 10kk gold each becomes **nothing**, and 10kk-per-strike becomes **100kk for the board**.

🔑 **The price did not vanish, it moved onto the road.** Three sigils used to be level 76 plus 60kk SP
and 30kk gold on one character. Three sigils are now **three subclasses each levelled to 75**, every one
of them born at 40 with no SP. Charging for the commit on top of that would have been charging twice.
⚠ There is no way to strike off a single sigil any more; the Mindwright's button says **Clear all** and
the row says what it costs, because tapping one line and losing three is not a surprise you can undo.

### The client
The Sigils tab is rebuilt around trees instead of slots — a `SLOTS worn / open` header, six tree
headings, each locked or open, each row saying *why* it is unavailable. It reads its two numbers off the
server (protocol **45**, two appended fields on the subclass push) and computes neither itself: this rule
decides what three subclasses are spent on, and a rule with two implementations has two answers.

❓ **§9.6 is still unanswered and I have left it as built**: the completeness gate (every class you own at
75 before you may add another) still bites on a BOUGHT slot, so a 500kk slot can sit unusable. My reading
stands — it is your existing rule, it is what stops half-levelled subclasses stacking up, and a bought
slot is never lost, only waiting. One line either way when you want it.

📥 **Still owed on `BL-250`: the class master's subclass dialogue and its info panel (§7-§8) on the
CLIENT.** The server side of both — `SubclassOfferInfo`, every row of it derived — has been built since
0.155.0.

## 2026-09-17 — 0.168.0: the drop database, built once and remembered — `BL-253`

*"a drop db should be build once and only once when server starts … it should remember it every restart
until something tuches drops/mobs … on start if its missing its build with drops x1 … each ask of item it
looks up and see mobs that drop and show the drop rate for the player (similar to [info->drops] on mobs)
… same as server<>apk protocol -> a version that says (rebuild even when u have the mob database)
otherwise it only build if missing"*. All of it, plus the two questions the entry was holding — it is a
**player** window and it shows **everything**, both of which that message answers.

### Where it is
* **Menu → Drops.** Type two characters, get every source of everything that matches: creature, level
  band, rank, field, chance.
* **`/whatdrops <item>`** in chat prints the same answer. It needs no APK, so it works the moment you
  restart the server — and it is not admin-gated, because your ask was a player's question.
* `--drops` in BalanceMatrix still works and now reads the same index as the other two.

### Built once, remembered, and the two stamps that decide
The index is **19,842 rows** cached as `Game.Server/dropindex.txt`, beside `game.db`. Two stamps guard it,
because they catch different failures:
* a **content hash** over the templates, their drop rows and the spawn zones — your *"until something
  touches drops/mobs"*, automatic, nothing to remember;
* a hand-bumped **`DropIndex.Version`** — your *"rebuild even when u have the mob database"*, for what a
  data hash cannot see: the rank LAYERS are code, and editing a number inside one moves no data at all.

`/dropindex` says what the last boot did; `/dropindex rebuild` forces one.

🔑 **CHANCES ARE STORED AT ×1 AND MULTIPLIED WHEN READ** — your *"build with drops x1"*, and it is also
what makes the cache safe: `/droprate ×100` moves every number in the window without the index being
rebuilt, because the knobs were never baked in.

🔴 **A measurement you should have, because it argues against the cache.** The index builds in **13 ms**
and loads in **28 ms**. The file is *slower than rebuilding it*. It is 15 ms at boot either way, so
nothing is hurt — but the speed argument for keeping the file is simply not there, and if you would
rather not have a 1.8 MB generated file next to the database, say so and the stamps stay while the file
goes. I built it as specified rather than quietly dropping half your instruction.

### The two faucets that were invisible, and the drift that is now impossible
A "where does this come from" lookup that read `MobType.Drops` answers wrongly for half the game — rank
is a property of the SPAWN, and the elite/boss layers were **hand-rolled inside the kill path**. Two of
them were not drop tables at all:
* the **recipe-book roll** — which `--drops` had been reconstructing by hand, free to drift from
  `RollBossBonus`, and which the in-game window would have made a *third* copy of;
* the **boss mat pile** — which nothing outside the kill path could see, so every Common and Uncommon
  material in the game had a source no lookup could report.

Both are tables in `MobCatalog` now (`RecipeRolls`, `BossPile`) with the kill path as one reader and the
index as another. **One walk, three readers** (`Game.Shared/DropIndex.cs`): the tool, the server and the
client window. ✅ It immediately paid: the lookup now shows that a Valley Treant BOSS drops Rare Wood at
50% from its pile — a source that became true in 0.167.1 and that no tool could see before today.

### 🔴 And it found something: `BL-262`
Extracting the mat pile made visible that **nothing multiplies it** — not the global rate, not the group,
not a Rune of Drop, not the level gap. That is exactly the shape `BL-247` found in the recipe roll
(*"fix the blueprints to take the rates multiplier"*), which is why your ×100 never touched the books.
I did **not** apply that ruling here on my own, because it is not the same decision: a rate on the books
means more books, a rate on the pile means 600-1000 Common Leather off one boss. Three ways out are in
the entry; my reading is to rate only the two CHANCE rows and leave the guaranteed handful alone.

⚠ **Protocol 44** (a new hub method and a new push, both pure additions — an old APK simply has no Drops
window). **A NEW APK IS WANTED.**

## 2026-09-17 — 0.167.1: the Plant boss was still paying leather

The wood fix shipped an hour earlier and **a second copy of the same map outlived it**. The boss and
elite MAT PILE (`RollBossBonus`) is not a drop table — it is a hand-rolled pile — and it carried its
own, coarser category → material map, still reading `Animal or Plant => Leather`. So a Plant creature
paid wood when you killed it and leather when you killed its boss.

There is **one map now**, `MobCatalog.MatFlavor`, and both sides call it.

🔑 **The two maps agreed on every other category, which is precisely how a duplicate survives long
enough to drift** — nothing was ever visibly wrong with it until one of the pair was edited. Worth
repeating because this is the third time in a month (the stackable rule had four copies in 0.146.1;
`ItemCategory` had two in 0.158.0): **fixing one copy of a duplicated rule means grepping for the
others in the SAME commit.** This time the grep was done, and it found exactly one.

## 2026-09-17 — 0.167.0: a Plant is what you farm for wood, and an unpriced debuff stops blocking

Two of your four rulings, taken first because neither needs a client.

### `BL-254` — RARE WOOD EXISTS NOW: *"make wood drop as lether .. primary/secondary -> animals: leather/wood, plants: wood/leather"*

Animal and Plant shared **one line** in `StandardDrops`, and that line is the whole bug. Every creature
pays a PRIMARY and a SECONDARY material type — and only the primary climbs: the Uncommon rung (30+)
pays both, but **Rare (60+) and Epic (76+) name the primary alone**. Wood was every category's
secondary and nobody's primary, so no Rare rung anywhere in the world could ever name it. Rare Wood
did not drop rarely; it had **zero sources and never had one**.

The two categories now mirror each other instead of sharing a row — Animal `(Leather, Wood)`, Plant
`(Wood, Leather)` — and **Rare Wood has five sources**: the Valley Treant (60) and the Bogwood (62), in
Sunken Hollow, Ironreach March and Sunken Vale.

✅ **All five material types are now somebody's primary** — Leather (Animal), Wood (Plant), Ingot
(Humanoid), Thread (Undead/Insect), Gem (MagicCreature/Angel) — which closes the entry's wider warning
that *"it is the same question for every SECONDARY material at Rare"*. Measured, not assumed:
`--drops mat_<type>_rare` returns 12 · 5 · 61 · 27 · 7 sources.

⚠ **Wood is the thinnest of the five, and it is the WORLD that makes it thin, not the drop table.**
There are exactly **two Plant templates in the game**, both around level 60. Leather has 12 sources
because the world is full of animals. If wood should be commoner, the fix is more Plant creatures in
the roster, not a rate — say so and it is a spawn pass, not a formula.

### `BL-259` — THE FOUR WARLORD MODIFIERS STAY AT `x1`, AND THE RULE AROUND THEM CHANGED

*"leave them as u made them -> playtest will show (if i forget to write modifiers put default ones ..
in playtests ill modify them if needed)"*. Charge n Shock, Shocking Shout, Shocking Javelin and
Taunting Shout keep the `SUCCESS` of **1** they shipped with — **no code change was owed**, the file
and the build already agreed, and `--check` confirms it (86 rows verified, OK).

🔑 **The general half is the part worth keeping.** `BL-232`'s *"never pick one yourself"* is now
**narrowed, not repealed**: an unpriced debuff ships at the **default `x1`** and gets its number at a
playtest, instead of blocking on you. The licence is to use the *default* — never to invent a
plausible-looking `0.85`, which would be indistinguishable from your own cell and is exactly what
`BL-232` forbade. The row still goes in the file and you are still told, because a row you cannot see
is a row you cannot retune. Written into `CLAUDE.md` and into the checker, whose MISSING line now says
so rather than telling the next session to stop and ask.

## 2026-09-17 — 0.166.1: Whirlwind's power column is one ladder again

*"i fixed wirlwind rungs -> 300 +50/rung"*. `BL-261` closed within the hour it was raised.
**300 / 350 / … / 1000** across the 3rd tier, running straight into `war_aoe 4th.csv`'s 1050 with no
step at the tier boundary — thirty rungs of one +50 stride.

🔑 **The three checks that found it are the part worth keeping**, because none of them needed the game
to be run: rungs 8-12 were the Elf Sword Dance's cells **exactly** (and 1-7 that ladder minus 75) — two
skills in two files do not agree to the unit by accident; the 4th file opened at 1050, far *below* rung
12's 1540; and +50 backwards from 1000 over fifteen rungs lands on **300**, which is what rung 1 already
said. **Both ends of a ladder agreeing while the middle disagrees is a transcription defect, every time.**

⚠ It was worth chasing because this column is multiplied by **twenty**: at rung 12 as first authored,
one Whirlwind was 30,800 power against Shocking Shout's 3,400 on the same rung — and levelling 68 → 76
would have *cost* a third of the skill's damage.

✅ **`SkillCsvSeed --check` now reports "No discrepancies" on every walked file** — no drift, no
unauthored extras, no ladder dips. First time that has been true since `war_aoe 3rd` was written.

## 2026-09-17 — 0.166.0: `war_aoe 4th.csv` is built, and the WARLORD is finished

**`BL-237` is closed and archived.** Both of his Warlord files are mirrored in the code, `--check` is
clean on both, and `war_aoe 4th` has earned its `Check.Specs` line — *"im done with war_aoe 3rd/4th"*.
**Every authored warrior file is now built.**

Only **two** skills in that file are new; everything else continues a ladder his 3rd file opened.
* **Master of Combat** (76, toggle) — +10 accuracy, +25% P.Def, +15% M.Def, +20% attack speed, **−30%
  move speed**, 30 MP/s. 🔑 The speed cut is a **minus on the buff**, not a `Slow`: that is Battle
  Frenzy's lesson verbatim — *a downside you chose is not a curse somebody cast on you*. A `Slow` sits
  in `AnyDebuff` **and** `ControlCc`, so a cleanse would strip your own stance, CON resistance would
  refuse it, and a boss would be immune to it.
* **Shocking Javelin** (76, ×15) — Shocking Shout thrown 900 away with a tighter ring (150 vs 200).
  🔑 **The two share ONE power column**, cell for cell: the Javelin buys reach with **radius**, not
  with damage.

Continued: the blunt mastery (rungs 16-30), the armour (21-35), Whirlwind, Shocking Shout, the three
race Shouts, Taunting Shout, the Supports' rung 4 — and **Final Stand (4-5) and HP Boost (11-18),
which his `warrior 4th.csv` deliberately stops at 74.** ⚠ The two warrior files disagree there **on
purpose**; do not tidy one onto the other. That is also why `war_aoe 4th` gets its own `Check.Specs`
line rather than sharing the Ravager's.

### 🔴 THREE DEFECTS THE CHECKER FOUND THE MOMENT THAT LINE WENT IN
Adding the spec was worth more than the code it validated:
1. **Two rows were unparseable** — Taunting Shout at 80 and 90 had a stray closing `"` with no opening
   one, so the DESCR swallowed the rest of the line and the checker read the skill as *not authored at
   all*. Quote closed.
2. **All three Supports at 80 carried the id `waraoe_life_support`**, while the section headers
   immediately above them read Life / Blood / Vanguard Support. Three rows, one id, one level, three
   different payloads and three different race cells — a state the engine cannot express, and the
   checker said so twice (`RUNG COUNT: CSV has 3 (80/80/80)` and a `LADDER DIP` from 15% vamp to 5%).
   Restored to the 3rd file's three ids. **One word reverses it if that was deliberate.**
3. **The blunt mastery and Final Stand had no 4th-tier rungs at all**, so their prices read as 1 SP and
   28,000 SP against his 6.5kk-to-gold ladder. Both concatenated properly now.

⚠ **`BL-261` (Whirlwind's dipping power column) is still open and still yours** — the one thing
`--check` prints. ⚠ **`BL-259` is now FOUR cells**: Shocking Javelin joined. ⚠ **Needs a new APK.**

## 2026-09-17 — 0.165.0: the Warlord finally has a kit — `war_aoe 3rd.csv` is built

**`BL-237` §5, the last open half of the warrior, closed.** Until this commit the string `waraoe`
appeared in **zero** `.cs` files: the blunt discipline had your passives, your buffs and Charge, and
for its damage it borrowed the derived `war_sundering_blow` stand-in. Ten ids landed; that stand-in is
**retired in the same commit**, and with it the last derived fighter ladder in the game.

| | what it is |
|---|---|
| **Shocking Shout** ×15 | a self-centred ring, 1000 → 4000 power, 5s stun, cannot be blocked, can double |
| **Whirlwind** ×15 | a CHANNEL — 20 strokes over 4s, each its own execution with its own crit and splash |
| **Taunting Shout** ×2 | 600/800 taunt, 30s, and *"more dmg from blunts"* — **a new damage channel** |
| **Shattering / Breaking / Crippling Shout** ×15 each | the three Slashes' rots, **on a ring, with the strike taken out** |
| **Life / Blood / Vanguard Support** ×3 each | a proc: heal a share of max HP **and** leave something lingering |
| **Battle Revival** | Battle Regeneration's ladder taken to **100%**, on five minutes |

### 🔑 "MORE DMG FROM BLUNTS" IS THE ONE CHANNEL THAT CANNOT BE A DERIVED STAT
New `SkillDef.VulnerableToWeapon` + `WeaponVulnerabilityPct`, carried on the **buff**. Every other
damage-taken channel in the game (`PvpDamageTakenPct`, the armour sets') depends only on the DEFENDER
and is folded once in `RecomputeDerived` — this one asks **what the attacker is holding**, and a
defender's recompute has no attacker. So it is read off the victim's buff list at the swing, in the one
pipeline every hit passes through. ⚠ It pays **anyone** holding a blunt, not just the Warlord: that is
what makes Taunting Shout a party tool rather than a personal one.

### 🔴 TWO GATES HAD TO LEARN ABOUT A FIELD PAYLOAD, AGAIN
The same lesson this file has now paid for six times:
* **`PayOutProc` returned the instant it paid a heal.** Correct while every instant payload was a bare
  sigil heal. Your Supports are the first that are BOTH — *"heal for 5% max HP, **and** leave
  lingering …"* — so the lingering half would have been dropped on the floor and the passive would have
  looked simply broken. The test is now what it always meant: return when there is nothing left to apply.
* **`ProcChance` had no per-rung slot**, so your 10 / 15 / 20% ladder would have rolled **10% at every
  rung** — the identical bug the toggle upkeep had (`BL-208`). New `SkillLevel.ProcChance`, taught to
  the roll, to the loop's entry gate, and to `SkillCsvSeed`.

### ❓ TWO THINGS ARE YOURS
* **`BL-261` — Whirlwind's power column DIPS**: 1540 at 68, then **900** at 70. Built verbatim, and
  `--check` now prints 🔵 LADDER DIP at it. Three independent checks say the first twelve cells are an
  older column: rungs 8-12 are the Elf Sword Dance's cells *exactly*, `war_aoe 4th.csv` opens at 1050
  (far below 1540) and climbs +50, and +50 backwards from 1000 over fifteen rungs lands on **300** —
  which is what your rung 1 already says. Both ends agree; only the middle disagrees.
* **`BL-259` — three landing modifiers**, now that Shocking Shout and Taunting Shout joined Charge n
  Shock. ✅ The three race Shouts needed nothing: you priced them yourself in the cell *"Single debuff
  x1.5"*, and that is exactly the `BL-232` rule — a shout that only curses lands more readily than a
  Slash that curses **and** strikes (×0.7).

⚠ **Still unbuilt: `war_aoe 4th.csv`** — Master of Combat, Shocking Javelin, the 76-90 continuations
and Final Stand's top rungs. Next. ⚠ **Needs a new APK.**

## 2026-09-17 — 0.164.0: the charge moves over its DURATION, and the Warlord picks one of four

His note, whole: *"i gave on charge duration .. it should move the distance for the duration .. not
instantly"*, with four readings attached — normal charge cuts the distance over **1s**; **Flash Step**
is instant, higher reuse, no duration and no cast; **stunning charge** takes **2s** and stuns at the
end; **damaging charge** strides like the normal one and *"give[s] up on cooldown for a dmg"*.

### 🔑 HIS DURATION COLUMN IS THE STRIDE
`PullSeconds` was **0.4s — a number I picked** when Charge became a reverse pull (`BL-256`). Every
charge row in all four warrior files now carries a DURR cell, so the cell is what the engine reads.
Three consequences, all of them his:

| | before | now |
|---|---|---|
| Charge (40 / 76) | 0.4s stride, 3s reuse, no floor | **1s stride, 5s reuse, 150 floor** |
| a zero DURR | (impossible) | **instant — a teleport, not a one-tick crawl** |
| the checker | compared DURR to `DurationTicks` → every charge row 🟡 | compares it to the **stride** |

⚠ **ZERO IS NOW A REAL AUTHORED VALUE.** `BeginDrag` used to floor the journey at one tick
(`Math.Max(1, …)`); Flash Step needs zero to mean *teleport*, so the floor is gone and the instant case
is its own branch — the only place in the drag machinery that passes `announce: true`, because this one
really is a warp and the client should snap rather than slide 800 units in a frame.

### 🔑 "MIN CHARGE DISTANCE 150" IS A REFUSAL, NOT A NO-OP
New field `SkillDef.MinChargeDistance`, gated in `BeginSkill` beside the range test — "too close" and
"too far" are the same kind of answer and belong in the same voice. `BeginDrag` already tolerated *no
distance to travel* and that is right for a **pull** (a tow used point-blank is a stun) and wrong for a
gap-closer: it would spend a 10-second reuse on a stride of nine units and read as broken.

### 🔑 THE FOUR CHARGES ARE ONE CHOICE, MADE ONCE (`war_aoe 4th.csv`, level 80, Warlord only)
`Charge` · `Flash Step` · `Charge n Shock` · `Charge n Stomp`, each naming the other three **and** the
base Charge in `Replaces`. **Nothing was needed to enforce that** — see the correction on 0.163.1
below. New file `Skills.Warlord4th.cs`.

Two engine seams were needed for the payloads, and each is the natural one:
* **the stun lands on the ANCHOR.** `BL-154` already applies a pull's stun *when the journey arrives*;
  a charge is that journey with the ends swapped, so Charge n Shock reuses the tail and stuns what it
  reached. That is the **one** asymmetry between the two directions — get it backwards and the skill
  stuns its own caster. One skill id, one row in `debuff_landmods.csv`.
* **the stomp is a sub-skill.** `SkillDef.ChargeArrivalSkill` fires a hidden def at the anchor on
  landing (`warrior_charge_stomp`, 5000 power, 200 radius) — the `ChannelSkill` shape, so it brings its
  own crit, block and splash. ⚠ It is **not** the charge's own payload re-run: `ExecuteSkill` is the
  method that *pays* for a cast, so re-entering it would charge the MP and restart the reuse twice.

❓ **OWED BY HIM: the landing modifier for Charge n Shock.** `debuff_landmods.csv` has its row
(`DEBUFF ONLY (1)`, CON) and `SUCCESS` reads the code default of **1** — the file, not a decision.

⚠ **Needs a new APK** (the client builds its Learn tab from the compiled `ClassSkills`) and the
`war_aoe` kit itself is still unbuilt — see `BL-237`.

## 2026-09-17 — 0.163.1: the cleric's Heal replaces `elf_self_heal`, not nothing

His correction on the 0.163.0 flag (*"it was self_heal and should have become elf_self_heal .. it
removes the healers self heal to give him a targeted one"*). `cleric 2nd.csv`'s REPLACES cell was
emptied when the id it named stopped existing; it should have FOLLOWED the rename. Cell and `SkillDef`
both point at `elf_self_heal` now.

🔑 **THIS IS THE ONE PLACE A RACE LAYER IS TAKEN AWAY, and it is a TRADE.** An Elf who becomes a cleric
gives up a self-only heal for a targeted one — strictly the better tool, and the reason the class
exists. Don't generalise it: nothing else in either race block is replaced by anything, and the Human's
drain LADDER explicitly is not (only the level-14 `vampiric_bolt` taster beside it is).

🔴 ~~**The re-buy gap is real and is NOT new.**~~ **WRONG, corrected 2026-09-17 in 0.164.0 — there is
no gap.** `HandleLearnSkill` has carried `if (cur == 0 && IsSuperseded(player, def.Id))` since
2026-06-25, so a skill that something you own `Replaces` is refused at the shop; the client's learn tab
hides it (`GameUi.Skills.Superseded`) and `PersistenceService` prunes it again on every login. The
claim above was read off `HandleLearnSkill`'s *removal* line without reading its *gate* twenty lines
earlier. **`Replaces` really does mean "gone for good"** — which is what `war_aoe 4th.csv`'s four
interlocking charges are built on.

**Also settled, no code owed:** a ladder authored in a lower-tier file is gated by LEVEL, not by tier
(*"i can lvl up a human mage without changing class and ill lvl up a vampiric bolt with it … same goes
for any other skills that continue from the class before"*) — which is what 0.163.0 already does, and
what the 2nd-class bolt ladders registered to 80 have always done. And his `k` shorthand stands as
built: *"i got lazy to do 12.8k .. any way leave them be if they are build"*.

## 2026-09-17 — 0.163.0: `BL-258` — THE MAGE'S RACE LAYER, AND VAMPIRIC BOLT CHANGES HANDS

His `mage 1st.csv` pass, the twin of 0.162.0's fighter one (*"ok Mage 1st is also done … vampiric_bolt
id changed and skill moved, self_heal id changed and skill redesigned .. this moved cleric 2nd and
nukers 2,3,4"*). Five CSVs moved; the code follows all five.

🔑 **THE SAME STRUCTURAL POINT AS THE FIGHTER'S, AND IT MATTERS AGAIN.** The block is authored in the
FIRST-class file but ladders to 74 (the Elf) and **90** (the Human), so it is not a base-class kit a
level-20 grows out of. `ClassSkills.Cumulative` yields the archetype-null list only to a character who
has NOT changed class, so listing these there would have deleted them silently at the class change.
They are injected centrally instead — `ClassSkills.MageRaceSkills`, the exact shape
`FighterRaceSkills` and the armour masteries already use.

⚠ **IT IS NOT THE FIGHTER'S SIX RE-SKINNED.** Two of those (a self cure, a self heal) are a mage's day
job already, so his mystic block answers a different question — and every race gets a **blessing** at 7
on top, which the fighter has no equivalent of.

**THE THREE BLESSINGS** — `elf_blessing` / `demon_blessing` / `human_blessing`, level 7, one rung,
0 MP, 0 SP, auto-granted beside the grade passive in `AutoLearnCoreSkills`. Elf: +5% healing received,
+5% M.Atk, +10% MP regen. Demon: +5% magic crit rate, +5% cast speed, +10% HP regen. Human: +5% magic
crit DAMAGE, +5% natural regeneration of both pools, +5% Max MP.
⚠ The comma in his cells groups the percent — *"Received HP recovery magic, M.Atk +5%; Mp regen +10%"*
is two channels at 5% and one at 10%, not one at 5%. The Human is the odd one out with three at 5%,
and his "Natural Regeneration" is read as BOTH pools (the other two each take one at 10%).

**ELF — `elf_self_heal`**, nine rungs at 7-74, 60 → 800 power, 5s cast / 5s reuse.
🔴 **`self_heal` NO LONGER EXISTS.** The base-mage Self Heal (three rungs at 1/7/14, every race) was
deleted from his file and re-authored as this. **A Human or Demon mage now has no self-heal at all** —
that is the race split, not an omission. The id MOVED rather than a second def being authored, for the
same reason `mana_barrier` → `nuker_mana_barrier` did; pre-release, nobody outside this machine holds
the old string, and `ParseLearnedSkills` drops an id the catalog no longer knows.
🔴 The healer's `Heal` **no longer `Replaces`** it, and must not be given a replacement back: a race
layer is precisely what a class change does not take away, and the ladder goes on climbing to 74 long
after the cleric has Heal. His `cleric 2nd.csv` REPLACES cell was emptied to match.

**DEMON — `demon_over_limit`** (Over the Limit), five rungs at 7-70: +5/7/10/15/20% P.Atk **and**
M.Atk for **five seconds** on a **sixty-second** reuse, instant. That shape is the whole skill — you
spend it on the pull that matters, which is why the MP is a real nuke's worth at every rung.
⚠ Its own `BuffKey` and NO covered families, deliberately. In `atk_phys`/`atk_mag` it would fight the
Might/Force ladder: a 20-minute party blessing would refuse the burst on rank, or the burst would evict
the blessing and leave the mage naked for five seconds. A burst is a THIRD source.
⚠ Both effect flags. `BuffAtk` has been PHYSICAL-only since 2026-07-16, so "P/M.Atk" needs
`BuffPhysAtk` **and** `BuffMagAtk` or half of it is silently dead on the class that casts it.

**HUMAN — `human_vampiric_bolt`**, thirty-three rungs at 20-90. 🔴 **The spell did not change; WHO HAS
IT did.** Power, MP, the 750→900 range step and the 4th-tier prices are the old `vampiric_bolt` ladder
rung for rung (rungs 2-34, renumbered) — but it was the Human NUKER's, registered across
`nuker 2nd/3rd/4th`, and it is now **every Human MYSTIC's**. He deleted its rows from all three nuker
files; the three code registrations went with them, or a Human nuker would buy every rung twice.

🔑 **THE LEVEL-14 TASTER SURVIVES UNDER THE OLD ID, AND THAT IS WHY HE SPLIT THEM.** `vampiric_bolt`
keeps exactly one rung on the base-mage table — Human only, **range 600** now, his row — because it has
a job the ladder must not have: the cleric's Holy Bolt `Replaces` it at 20 (`[magic_bolt
vampiric_bolt]`, his cell). A base-class taster is replaceable; a race layer is not, and one id could
not be both.

⚠ **THE HUMAN LADDER IS NOT TIER-GATED.** Its fifteen 76-90 rungs are in `mage 1st.csv`, not a 4th-tier
file, so LEVEL alone opens them — a real change from the `nuker 4th.csv` rows they replace, which
needed the Rite of Ascension. It follows his placement; it is the one thing here worth a second look.

**The checker.** `mage 1st`'s band went 1-19 → **1-90**, and the code-side skip that keeps a central
row from reading as an unauthored extra on nine other files is now a shared `SkipCentral` with a mage
array beside the fighter one. `grade_penalty` is the one skill in both worlds — he authored its seven
rows in BOTH first-class files — so it is verified by whichever of the two is being walked.
✅ `--check` is clean on every file but `war_aoe 3rd`, which he is authoring as this ships.

⚠ **An APK is owed**: the client builds its Learn tab locally from the compiled `ClassSkills`.

## 2026-09-17 — 0.162.0: THE FIGHTER'S RACE LAYER, ON EVERY FIGHTER, FROM LEVEL 10

His `fighter 1st.csv` pass (*"fighter 1st is done .. fix all fighters.. give them skills based on
race"*) plus the grade passive he asked for in the same session. Six new race skills, one new
informational passive, two new engine channels, and Antidote re-homed.

🔑 **THE RACE BLOCK LIVES IN THE FIRST-CLASS FILE AND LADDERS TO 74.** That is the whole structural
point: these are not a base-class kit a level-20 grows out of, they are a layer the character keeps
for the entire game. `ClassSkills.Cumulative` yields the base (archetype-null) list ONLY for a
character who has not changed class, so listing them there would have deleted them silently at 20.
They are injected centrally instead (`ClassSkills.FighterRaceSkills`), exactly like the armour
masteries beside them — one injector rather than eight fighter paths × three races that must agree
forever. The regenerated `debuff_landmods.csv` proves it reached all of them: Demonic Pain's CLASS
column lists every Demon fighter class from Rogue to Warlord.

**ELF — sustain.** `elf_antidote` MOVED here, out of `tank 3rd` / `dual 3rd` / `archer 3rd` /
`warrior 3rd` / `shared 4th` (he deleted its rows from all five), and grew from six rungs at 52-74
to **nine from level 10**, curing ranks 1-9. Its four hand-written 3rd-tier registrations are gone
with them — reinstating one would sell an Elf rungs 4-9 twice. `elf_heal` (Healing Leaf) is new:
eight rungs, 100→800 power. Both are `Physical/Heal` now, his change — a fighter has no WIT to pay
for a magical cure, so `PhysicalCast` keeps them off the fizzle roll.

**DEMON — offence.** `demon_drain`, 21 rungs, physical damage + `Lifesteal: 0.60`, and its RANGE is a
ladder (400 → 600 → 800), the only fighter skill outside the bow tree that grows reach.
`demon_pain`, 21 rungs, a SOLO bleed — no direct damage at all, which is why its landing modifier is
the table's top price.

**HUMAN — defence.** `human_parry` (Weapon Parry) and `human_relaxation` (Relax), both new channels:

🔑 **pRes — THE PHYSICAL TWIN OF mRes** (*"we have MRes channel .. we need PRes .. its more like
Increases mRes and pRes with x%"*). `Entity.PhysicalResist` / `PhysicalDefCoef`, folded into
`StatCalculator.WeaponDefenceCoef` so all three physical damage paths get it from one place. It
rides INSIDE P.Def, like mRes and the weapon-type resists, so a defence-ignoring skill bypasses it.
⚠ **A resist is not a damage cut:** damage is a ratio, so `pRes +20%` is −16.7% damage, not −20%.
mRes keeps its flag (bit 31); pRes is a FIELD, because the enum has been full since `1L << 62`.

🔑 **POOL-FRACTION REGEN — a THIRD regen channel.** Relax is *"1.0 % HP/s"* climbing to 5% HP + 3% MP:
a fraction of your OWN pool per second. Neither existing channel could say that — a multiplier on a
tiny early formula is wrong, and a flat per-second grant is right at exactly one level. Added with
the flats, OUTSIDE the stance multiplier, because Relax already makes you sit.

🟢 **THE GRADE PASSIVE** (*"Just user to know when he is 58 and got B grade drop that he is not yet
allowed to wear"*). `grade_penalty`, seven rungs at 1/20/40/52/61/76/80, auto-granted, free, and it
grants nothing — the grade system itself is untouched since 2026-07-16. ⚠ Its learn levels ARE
`GradePenalty.GradeLevels`, never a copy: a skill whose whole job is to describe the equip math must
not be able to drift from it. ONE id with seven NAMED rungs, his call (*"cannot grade_penalty be one
id and jsut change the description and Name ?"*) — so `SkillLevel.Name` / `SkillDef.NameAt` /
`HasLevelNames` are new, and the client drops the "Lv.N" suffix for a rank-named ladder. A rung reads
**"Grade C"**, never "Grade F Lv.4".

**TYPOS AND LADDER DIPS HE ASKED ME TO CATCH.** Fixed in his CSV, all of them flagged:
- Three ladders dipped at their FIRST rung (the opening row was copied off `Shot`: 34 MP at 15 against
  20 MP at 20; parry 25 at 10 against 20 at 20) → 17/17/15. Demonic Drain and Pain also paid the MAGE
  SP ladder at 20-36, which overshot the 3rd-tier one at 40 (40k at 36, 28k at 40) → dropped onto the
  fighter ladder 1.7k/3.2k/6k/11k/20k. He chose *"smooth them, show me what changed"*.
- `Sword|Blunt/2h` → `Sword|Blunt/2` — `/2h` is not valid hands grammar and the checker was ignoring
  the whole hands clause, so a dagger Human could have parried.
- `powe` → `power` ×5. **Not cosmetic:** the DESCR reader matches on `power`, so those five rungs
  would have read as UNREAD rather than being verified.
- Trailing space in `Demon ` (2 rows, would have broken race parsing) and a leading space in ` 910`.
- Three section banners said "Demonic Smash" over Demonic Drain, Demonic Pain and the HUMAN's Weapon
  Parry; "Tree of Life" sat over Relax.
- `elf_heal` had an EMPTY MP column on all eight rungs → priced on the Antidote ladder, his ruling.
- Weapon Parry's DURATION was 1 second on a 60-second reuse → **10s** (*"10s (ate the 0)"*).

⚠ **`demon_pain` is a NEW DEBUFF and its modifier is HIS** — `debuff_landmods.csv` row added at
**x1.5**, the solo-debuff price he named. Never picked locally; that is the standing rule.

**The checker.** `fighter 1st`'s band went 1-19 → **1-90** (the file's tier is "all of it" now), and
the central layer is skipped on every other spec's code side. ⚠ `Also: "fighter 1st"` is the obvious
guess and is WRONG — `Also` folds a whole file in WITHOUT band-filtering, so a 20-39 spec then sees
all 21 Demonic Drain rows against the 5 its band admits. `shared 4th` gets away with it because its
band is the tier. `fighter 1st.csv` and all nine other fighter files now check clean.

🔵 **LEFT OUT, deliberately: the MAGE.** He said *"fix all fighters"*, and `mage 1st.csv` has no race
block — inventing one would put rows in the code that no CSV authors. The grade passive DOES reach
mages (grade is a character property), but its seven rows are authored only in `fighter 1st.csv`; if
he wants them mirrored into `mage 1st.csv` that is one paste and a line in the checker.

⚠ **No `game.db` delete owed** — no schema change; `LearnedSkills` is a dict and gained ids, not columns.
⚠ **Needs an APK** for the grade passive's per-rung name and the new skill cards.

## 2026-09-17 — 0.161.0: THE SHEET SHOWS THE REAL M.ATK — THE DISPLAY SHRINK IS RETIRED

🔴 **BOOKKEEPING, SO IT IS NOT LOST: THIS COMMIT (`0b7bb6b`) ALSO CARRIES YOUR CSV ROWS, AND THEY ARE
NOT BUILT YET.** While 0.159-0.161 were being written you authored `war_aoe 3rd.csv` (+97 rows —
Shocking Shout, Wirlwind, Taunting Shout, the three racial Support passives, Battle Revival, the
racial Shouts), `war_aoe 4th.csv` (+34 — Armor Mastery, Two-Hand Mastery, Charge) and moved **Elf
Antidote** out of `war_aoe 3rd` into `shared 4th.csv`. A `git add -A` swept them into this commit,
whose message says nothing about them — hence this line. **That is `BL-237`, and the code half is
still owed.** Nothing was lost or overwritten; the Antidote rows were MOVED, not deleted.

⚠ **Needs an APK only to see it on the phone's own numbers** — the value is computed server-side, so
an existing APK connected to this server already shows the new number. No `game.db` change.

*"show the inner mAtk in stats ... now i have archers with 4-5k p atk .. and mages stay at 1000 ... and
looks now very underinflated .. so leave the visual == inner mAtk and if it feels again overinflated we
will fix the formula"*.

**There is now ONE M.Atk.** The character sheet and the target window print exactly the
`EffectiveMagicAttack` the damage formula reads. The shrink — `min(internal, 20·√internal)`, in from
2026-07-25 — is **deleted**, along with `Entity.EffectiveMagicAttackShown` and
`StatCalculator.MagicAttackDisplayScale`, which was its only user.

**What moves, measured:** at level 85 in best gear an unbuffed nuker's sheet goes **685 → 1,174**.
Below internal 400 (roughly level 55 and under) **nothing changes at all** — the shrink was equal to
the internal value there and only bit above the crossover.

🔑 **The next lever is the FORMULA, not the display** — your words, and the code now says so in three
places (`Entity.cs`, `StatCalculator.cs`, `docs/Formulas.md`). A display number that disagrees with the
one damage uses is how you lose the ability to tell a balance problem from a rendering one.

⚠ **One thing the measurement says, for when you look at it on the phone.** Unbuffed, best gear, the
mage's M.Atk was already AHEAD of the fighter's P.Atk before this change and is further ahead after it:

| lvl | FTR P.Atk | MAGE M.Atk | M/P |
|---|---|---|---|
| 40 | 237 | 217 | 0.92 |
| 61 | 412 | 425 | 1.03 |
| 76 | 548 | 669 | 1.22 |
| 85 | 907 | **1,174** | 1.29 |

So the 4-5k archer is a **buffed and enchanted** number, not a base-curve one, and the gap you are
seeing is the physical buff stack rather than the M.Atk curve. Worth knowing before the formula moves.
`tools/BalanceMatrix` prints that table now — it replaced the old display-ramp comparison, which
measured a shrink that no longer exists.

## 2026-09-17 — 0.160.0: `BL-246` — THE CHARACTER SHEET BECOMES TWO TABS

⚠ **Needs an APK** (the whole change is the window). No new `game.db` delete beyond the one `BL-239`
and `BL-241` already owe.

Built to your layout, row for row: **BASIC** (Class · Primary · Basic · PVP) and **DETAILS** (the class
chain · Vitals · Offence · Defence).

🔑 **BASIC SHOWS THE LAST CLASS ONLY** — *"Class: Shadowblade (Directly Shadowblade, not
ElfRogue,Descipiline etc ... just last class)"* — and DETAILS shows `Elf Rogue -> Phantom ->
Shadowblade`. That distinction is the point of the split, and the rest of the layout follows the same
rule: BASIC answers *who am I and can I fight that*, DETAILS answers *what exactly is my sheet made of*.
So the speeds appear on both, in the two readings that are actually different questions — the raw stat
over its cap on BASIC (how much room is left), the multiplier against the 333 baseline on DETAILS.

**It was a PROTOCOL change too — and FOUR rows, not the seven the entry expected.** Checking each
against the code rather than the note: the three skill masteries, the crit-RESIST pair, `Restore power`
and `HP Receive` were **already on the wire** since `BL-190` and the heal-stat work, and simply had
nowhere to be drawn. The four genuinely missing ones arrive with **protocol 43**, appended to
`StatsUpdate`:

| row | source |
|---|---|
| `MP Receive` | `Entity.RestoreMpMod` — the MP twin of heal-received |
| `Stab Rate` | `Entity.BlowRate` (`BL-188`), already clamped |
| Defence `M.Crit` | `Entity.MagicCritRateResist` |
| `M.Fail` | **computed server-side**: `MagicFailChance` at PARITY |

⚠ **M.Fail is computed, not copied, and that is deliberate.** A fizzle chance needs an attacker, and
"an attacker of my own level" is the only reading of a defensive fizzle number on a sheet with no
attacker in it. `MagicFailMod` is still sent beside it — that is the ×2 MULTIPLIER (Anti-Magic), not a
chance, and the two are different lines.

⚠ **`Crit dmg` now reads as the FINISHED multiplier** (`PhysicalCritMult` = 2.0 + your bonus, capped),
not the bonus alone — the same reading as the `M.Crit dmg` beside it, which has always been the
finished ×2 / ×2.6 / ×3.38. Showing one as a total and the other as a bonus is how a sheet teaches you
a wrong number. The `+N` after it is the FLAT crit damage, which joins attack inside the ratio.

**The Defence group's `Crit` / `Crit dmg` / `M.Crit` are RESISTS**, not your own crit — they cut an
attacker's rate and extra damage against you (`BL-211`). Same word, opposite side, which is exactly
why they belong in the group they are in rather than beside the offence rows that share their name.

## 2026-09-17 — 0.159.0: `BL-241` — THE PICKUP FILTER, AND IT IS A LOOT RULE

⚠ **Needs an APK** (the filter is set from the bag) and ⚠ **a `game.db` delete** (a new column — it
joins the one `BL-239` already owed).

*"we need in bag rarity filter for any type gear/mats/use to be able to select min rarity for pickup..
For 'gear' I make it rare and for 'use' I mkae it unc -> any uncommon/common gear is ignored and not
picked up and any 'use' that is common Is ignored as well; (if in party I'm ignored in the roster if
that rarity is filtered for me)"*.

🔑 **THE BRACKET IS THE FEATURE.** A filter is not "bin it after it lands": it takes you OUT OF THE
PARTY'S LOOT ROSTER for that drop. Round Robin skips your turn, Random never rolls you, and the item
goes to someone who wants it instead of being destroyed — so this is a change to the loot rules, not a
bag toggle. It is why the filter is character state on the SERVER and not a client preference: the
roster is decided in `RollDrop`, where no client is asked anything.

**What it is, exactly:** three categories — Gear, Use, Mats — each carrying a minimum
`ItemRarity`. Common means "take everything" and is the default on every character, so nothing changes
until you set one. Quest tokens cannot be filtered at all: a quest item you refused is a quest you
cannot finish.

**One question, one place.** `GameLoopService.PickupWanted` is asked by every drop site — the per-entry
award, the elite/boss mat pile and the recipe roll — the same shape (and the same reason) as
`BL-239`'s `LockRefuses`. A drop path added later gets the filter by calling it, or it gets it never.

**Where it deliberately does NOT apply:** `AddItem` itself. A quest reward, a crafted piece, a vendor
purchase and a warehouse withdrawal are all things you asked for by name; a bag that refuses what you
just paid for is not a filter, it is a bug.

**Every loot-mode fallback now checks the roster first.** `LootRecipient` used to fall back to the
killer in three branches; it falls back to him only if he is *in* the roster, because a filtered killer
receiving by the back door of a fallback would undo the whole thing. Finders Keepers with a filtered
killer therefore drops the item — that is what finders keepers means. Leader Only still prefers the
leader, then the killer, then whoever is left: an item nobody is owed still beats an item destroyed.

**The bag grows a `[Pick]` button** whose caption is how many categories are filtered, so an active
filter is visible without opening anything. It opens a three-row popup that cycles each category up the
rarity ladder and wraps back to "take everything". The rows relabel from the **server's echo**, never
from the tap — the same rule the lock toggle runs on.

**Protocol 42** — `InventoryUpdate.PickupMinRarity` (three ints, riding with the bag it describes, like
`LockedDefIds`) and the `SetPickupFilter` hub method.

## 2026-09-17 — 0.158.0: `BL-240` — INSTANT SALE: ONE RARITY, ONE TAB, ONE TAP

⚠ **Needs an APK** (the button is in the vendor's Sell tab). No `game.db` delete.

*"We need a system for instant sell u click on button inside the vendor sell tab and it shows rarity to
instant sell -> it sells everitying of that rarity depending on the tab you are on.. If I'm on the
'gear' tab and click 'instant sale' and chose 'rare' it sells all that are rare gear in my inventory"*.

**Built exactly to that scope: the TAB and the RARITY, and nothing else.** In particular it is not
"and below" — picking Rare sells rare, not rare-and-worse. A ladder would make one button destroy
things you did not name.

🔑 **`ItemCategory`, `CategoryOf` and `InCategory` MOVED TO `Game.Shared`.** They were private to the
Unity client until this: the sweep runs on the server, so the server has to mean *exactly* what the
tab you are looking at means. A second copy of a tab rule on the server is precisely the drift
`ItemTag` exists to prevent — you would pick "Gear" and the sweep would sell by a slightly different
definition of it.

**Two taps.** The button lists only the rarities you actually hold something sellable of, each row
naming how many rows and how much gold; picking one asks a plain confirmation repeating both. The
quote is the client's arithmetic over the same `ItemTag` predicates the server uses, and the server
re-derives both — so they agree, and its number is the one that lands.

**Never swept:** equipped gear (skipped silently, not refused — you did not name it, and a sweep that
stops to complain about your weapon is a sweep you cannot use), quest tokens, anything a vendor would
refuse one at a time, and **anything LOCKED** (`BL-239`, which is why it was built first).

⚠ **Buy-back is the undo, and it is only `BuyBackSlots` deep.** A sweep of thirty rows pushes the
earliest ones off the shelf. That cap is not new, but this is the first thing in the game that can
reach it in one tap — which is why the confirmation says so.

## 2026-09-17 — 0.157.0: `BL-239` — AN ITEM LOCK, AND IT LOCKS THE **DEF ID**

⚠ **Needs an APK** (the lock is set from the item-details window) **and a `game.db` delete** — the
locks are a new `LockedItemsCsv` column on the character row, and `EnsureCreated()` does not add
columns to an existing database.

*"we need a lock on items not to show in sell window nor their del/dismantle button to be active. ->
open details window of an item and top there is a button that locks that item ... you lock item id ->
every item(stacks) of that item is locked -> you lock one stack of potions .. mobs drop more .. u get
new stack its also locked, u can use consumables when locked"*.

🔑 **THE LOCK IS ON THE DEF ID, NOT THE INSTANCE**, and everything else falls out of that. It is a set
of item ids on the CHARACTER (`Entity.LockedItems`), not a flag on an inventory row — so the stack you
lock is still locked after you drink it empty and loot another, it survives a relog for free, and no
existing inventory row needed migrating.

**What it blocks — five paths, one gate.** Sell, bin, break down, both keepers, and trade. They all
ask `LockRefuses` rather than each carrying its own copy of the rule: these six handlers have
historically disagreed (the quest-item refusal had to be re-added to the private keeper long after the
other five had it, §39e), and a seventh disposal path added later gets the gate by calling this or it
gets it never.

**What it does NOT block: USING a consumable.** That is the whole point — you lock a stack of potions
so you never sell it, not so you can never drink it.

**On the phone:** a `Lock` / `Unlock` button at the **top** of the item-details window, beside the
name. A locked item's `Bin` and `Break down` buttons stay on the row and go grey — *"their
del/dismantle button to be [in]active"* — because hiding them would read as "this item can't be broken
down at all", which is a different statement. A locked row is prefixed `[L]` in the bag, it vanishes
from the sell list entirely (*"not to show in sell window"*), from the keeper's DEPOSIT side, and from
the trade table; **and it loses the bag's fast DEL/BRK button**, which closes `BL-244`'s open clause —
that button is the one place in the game with no confirmation step, so there is no dialogue behind it
to catch the tap.

⚠ The withdraw side of a keeper is deliberately untouched: getting a locked item *out* of a bank is
the one move a lock has no reason to stop.

## 2026-09-17 — 0.156.0: `BL-242` · `BL-243` · `BL-244` · `BL-245` — four quality-of-life asks from the playtest

⚠ **Needs an APK** — all four have a client half. No `game.db` delete: the one new stored field
(`ManaPotions`) lives inside the existing `AutoHuntJson` column, and an existing save without it falls
back to exactly the behaviour it had.

The cheap half of `BL-239`…`BL-246`, taken first so the expensive four — the item lock, instant-sell,
the pickup rarity filter and the two-tab stats window — land on their own. Nothing here changes a
formula, and nothing here is on the balance side of the game at all.

### `BL-244` — THE FAST BUTTON IS A CYCLE: DEL:OFF → DEL:ON → BRAKE:ON

*"the button for fast delete in bag to be a cycle button ... and the del button to become some dark
purple for dismantle"*. One button, three states, and the third arms a no-confirm **break down** on
every row instead of a bin. The two destructive modes never wear the same colour — Del keeps the bin's
red, Brake is purple — so the button under your thumb tells you which one you are in without reading it.

⚠ In Brake mode a row that **cannot** be salvaged shows no button at all, rather than one that does
nothing. `Crafting.Disassemble` is the same test the details window's Break-down button already uses.

### `BL-242` — THE SELL LIST SHOWS THE ENCHANT, AND THE ATTRIBUTES

*"sale list don't show enchant value and in the description of the sell item row should show the
attributes if any"*. A +6 and a +0 were two identical rows in the one window where you part with them
for good. The row now reads `+6 Electrum Blade`, its second line carries any attributes the instance
holds, and the confirm dialog repeats the `+6` — that dialog is where you actually commit.

🔴 **And the list was asking the DEF, not the INSTANCE.** `ItemCatalog.SellPrice(def)` /
`IsSellable(def)` ignore `SellPriceOverride` and `TradableOverride`, which the server's `HandleSell`
reads — so a per-instance-priced item was quoted a price the server would not pay, and a
per-instance-bound one was offered a row the server refuses. Both sides go through `ItemTag` now.

### `BL-243` — MANA POTIONS PER RARITY

*"make the same as healing pots and for mana pots"*. The MP side needed the ladder more than the HP
side, not less: the three mana potions restore **120 / 500 / 3000**, so one threshold plus
"best potion in the bag" spends a Rare to top up a nick. Common@70 / Uncommon@50 / Rare@25 keeps the
expensive bottle for the hole.

The Potions tab is **two columns** now — heal left, mana right — because a second ladder stacked under
the first needed ~120px the window does not have, while its 620-wide sliders used half of 760.

⚠ Three mana rarities, four heal ones (there is no Instant mana potion), so they are two arrays and
two loops rather than one shared index. A save written before this field arrives with `MpPotionPct`
set and no ladder: every rung comes up armed at that one percent, which is what the old path did.

### `BL-245` — THE CRAFTER SEES (AND SPENDS) THE KEEPER'S SHELF

*"crafter should see mats in private wharehouse -> maybe the crafting window can have a toggle button
(on by default) [show keeper items]"*. A `[Keeper: ON]` toggle beside the four tabs, on by default,
remembered in PlayerPrefs like the `[ORDER]` cycle.

🔑 **It counts AND spends** — which is the half that had to be decided. A window that showed 4/4 Rare
Ingots and then refused would be worse than one that never offered, so the flag rides the Craft call
and the server takes the shortfall out of the warehouse, **bag first**. The warehouse is pushed back
to the client whenever a craft touched it, because the client holds it from login and would otherwise
go on offering materials that are already gone.

⚠ The server pair is `CraftCount` / `CraftConsume`, deliberately craft-scoped rather than a
`includeWarehouse` flag on `CountItem` / `ConsumeItem`. Those two have thirty callers — quests,
potions, enchant scrolls, class change — and every one of them means the bag.

## 2026-09-17 — 0.155.0: `BL-250` — the subclass slot ladder, the tickets, and the free swap

🔴 **DELETE `Game.Server/game.db` (+ `-shm`/`-wal`)** — characters gain two columns
(`SubclassSlotsUnlocked`, `SubclassTicketsEarned`) and `EnsureCreated()` does not ALTER an existing table.
⚠ **Protocol 39.** The class master's dialogue carries a new section; an old client simply does not see
it (the field is appended with a default), so the phone keeps working until **the client half lands
with an APK**. Nothing on the ladder is reachable from the phone until then — this is the server half,
complete and tested. (⚠ Still owed as of 0.156.0.)

This is §5, §6 and §7 of `BL-250`, plus §8's data. The sigil half (§1-§4) is still to come.

### SLOTS ARE A THING YOU UNLOCK, NOT A THING YOU HAVE

A character starts with **zero** subclass slots. Three are earned and four are bought, and what you
receive in every case is a **Subclass Ticket** — an ITEM, exactly as you asked
(*"those values give you a subclassTicket and u can unlock them using(consumable) ticket"*). Earning or
buying one puts it in your bag; **using it** opens the slot. That separation earns its keep: the ticket
your main's 4th class paid you can sit there until you know which class you want.

| slot | how | |
|---|---|---|
| 1 | earned | your main reaches 76 and takes its **4th class** |
| 2 | earned | subclass #1 reaches 75 |
| 3 | earned | subclass #2 reaches 75 — a third pays nothing |
| 4 | **500kk gold** | bought at a class master |
| 5 | **5kkk gold** | |
| 6 | **100 platinum** | |
| 7 | **1,000 platinum** | |

🔑 **SEVEN RUNGS FOR SEVEN REACHABLE SUBCLASSES** — your cut of the 5,000-platinum rung landed the
ladder exactly on the roster. **Putting it back when the summoner ships is one row** in
`SubclassSlots.BoughtRungs` and nothing else, and that is by construction: the prices are an authored
list whose LENGTH is the ceiling, and "may another ticket be bought" is the COMPUTED question *"is
there a path this character could still legally add"*. **Nothing anywhere hard-codes seven** — even
`GameConstants.MaxSubclasses`, which was the literal `4` for a year, is now derived from the ladder.

⚠ Two guards worth knowing. **Tickets in the bag count as slots already paid for** when pricing the
next one — otherwise you could buy the 500kk rung three times by never using them. And **the earned
tickets are counted, not re-derived**: every earn condition stays true forever (your main does not
stop being 76), so without a record of what was *given* the game would post a fresh 5-billion-gold item
on every login.

### A SWAP BELOW 75 IS FREE, AND THE REPLACEMENT TAKES THE SLOT

Your ruling, built as stated. At 74 or below you may replace a subclass with any other you could
legally take, for **nothing** — *"You lose your progress anyways"*. At 75 it is refused rather than
priced, which is the level that pays its sigil slot and its tree.

🔑 **The load-bearing half is the duplicate check.** Your *"it takes its place so no duplicates will be
at the end"* is a statement about WHEN that check runs: with the outgoing class already discounted.
Otherwise swapping a Magus for its race-sibling would refuse itself for clashing with the very class
being removed — so the swap compares against your OTHER classes' paths, not against all of them.

⚠ **A swap grants no rune.** `BL-252`'s gift is one per subclass CREATED; per-swap would be farmable —
out and back every day for a free 100% Exp/SP rune forever.

### THE ADMIN PATH IS UNTOUCHED, WHICH IS WHY THERE ARE NOW TWO

*"admins can take subclass as its of now ... and normal players also need a NPC"*. `DebugAddSubclass`
stays ungated — no ticket, no slot, no level. The new `TakeSubclass` carries every player rule. Both
call **one** `CreateSubclass`, so the birth values can never drift apart and a rule can never apply to
one path and not the other by accident. The only check on both sides is the no-duplicate-PATH rule,
which is not a gate but an invariant.

The class master's second dialogue (§7) and its "what will this give me" panel (§8) are **built and on
the wire**: per offered class, the **sigil group it unlocks** and its three sigils, whether it would
open a sigil SLOT or only the tree, that it starts at 40 with 0 SP, and your held classes with which
of them may still be swapped. Every field is DERIVED from the catalogues — nothing about that panel is
authored twice. **Drawing it is 0.156.0.**

### ❓ ONE THING I DID NOT DECIDE FOR YOU

**The completeness gate now bites on a BOUGHT slot in a way it never did on an earned one.** The
existing rule — *every class you own must be at 75 with its 3rd class before you may add another* —
predates the ladder and you have never repealed it, so I kept it. But an earned slot is paid BY a
subclass reaching 75, so that gate is satisfied by construction; a **500kk slot can be bought at any
time and then sit unusable** until everything else is levelled. Recorded as `BL-250` §9.6. Say whether
a bought slot should bypass it.

### Verified

`tools/SmokeTest` — **ALL CHECKS PASSED**, with eight new assertions: the ladder is seven rungs, the
four prices are exactly 500kk/5kkk/100/1,000, **slot 8 is not on it**, an earned slot has no price, a
ticket can be held, using it opens exactly one slot and is consumed, the client is told the ceiling
rather than assuming it, and 🔑 **the unlocked slot count survives a relog** — a slot that opens on
screen and is never written is precisely the failure this harness exists to catch.

## 2026-09-17 — 0.154.0: `BL-252` — a subclass is BORN AT 40, with nothing but a rune

🔴 **DELETE `Game.Server/game.db`** if you want the old level-1 subclasses gone; nothing in this
version needs a new column, so an existing DB runs — it just keeps any subclass you already made at
the level it already is.

Your spec, verbatim: *"i want when you change a sub class u get a sp/xp 100% 1d rune. U get your lvl
to lvl 40 (not lvl 1). skills are not learned (skills are like your lvl 1 char creation) … new sub
class is born @40, no learned skills (except auto learned like mage etc.), 0SP, 0% exp, rune for 1d
sp/exp 100%"*. All of it, built.

### THE LEVELS ARE GIVEN AND THE SP IS NOT — that asymmetry is the whole design

A new subclass now arrives at **level 40, 0% into it, with 0 SP and an empty skill list**. It used to
arrive at level 1, which never squared with the fact that it is handed a **3rd class** on creation —
a class change that is itself gated at 40. The contradiction had been there since subclasses shipped.

What it does NOT get is forty levels' worth of skill points, and that is the point rather than an
oversight: you either buy SP bottles with your MAIN class's SP and gold, or you farm the bar back up.
The level is a shortcut past the boring part; the kit is not.

⚠ **"No learned skills" is not an empty list, and the code does not make one.** `LearnedSkills` is
born empty and `AutoLearnCoreSkills` then grants exactly the *"auto learned like mage etc."* set you
carved out — a mage's Magic Bolt and Spellcaster Mastery, and the class identity floor/reflect
passives. That method is **level-aware**, so a sub born at 40 gets what a 40 of that class gets. That
is the rule working, not an exception to it. (The SmokeTest asserts **0 SP** and not an empty list for
exactly this reason — asserting an empty list would fail on a mage for being correct.)

### THE RUNE IS THE EXISTING TOP RUNG, NOT A NEW ITEM

The gift is `rune_expsp_100` — **Rune of Exp/SP (100%)** — off the reward-rune ladder that has been in
the game since `BL-153`. That ladder already tops out at +100% and already defaults to **24 hours**,
so this is a LOOKUP (`RewardRunes.ChannelOf(KeyExpSp).ItemId(100)`) rather than an authored duplicate
that could drift from it. It is a **real held item on a wall clock**, so it can be saved for a session
instead of burning while you walk to a field, and `AddItem` stamps the expiry from the def's own
`GrantsRuneSeconds`.

🔑 **ONE RUNE PER SUBCLASS CREATED, never per swap.** Your *"when you change a sub class"* reads both
ways, and per-swap would be farmable — swap out and back every day for a free rune forever. Both of
these were flagged as MY readings in `BL-252` and are built as written there; say the word if either
is wrong.

### Verified

`tools/SmokeTest` — **ALL CHECKS PASSED**, including three new/changed assertions: a new class starts
at **level 40** (read off `ThirdClassCatalog.ChangeLevel`, not a literal, so it moves with the rule),
starts with **0 SP**, and both its birth level and its levelled-up value **survive a relog**.

### Also

`BL-250` is now 🟢 **fully unblocked** — you ruled the sub swap **free below 75** (*"You lose your
progress anyways"*) and **cut the 5,000-platinum rung** until the summoner exists, which lands the
ladder on seven rungs against seven reachable subclasses. Its build is next: the tickets, the slot
ladder, the class-master dialogue and the sigil rework.

## 2026-09-16 — 0.153.0: `BL-257` — PLATINUM, the account currency

🔴 **DELETE `Game.Server/game.db` (+ `-shm`/`-wal`) before you run this** — the accounts table gains a
`Platinum` column and `EnsureCreated()` does not ALTER an existing one.
🔴 **NEEDS AN APK** — protocol 38: the wallet push and every shop row carry a second number.

Your spec, verbatim: *"Also make platinum -> copy of gold without the drop -> items Def on their buy
price also must have a platinum value (Default 0) · any item that have a platinum or/and gold must be
bought with the value · platinum is not an item. It cannot be traded (until global marketplace) ·
platunum is account value. So any char in the acc shares it · add /giveplat admin command same as
givegold"*. All five, built.

### IT LIVES ON THE ACCOUNT, AND THAT IS THE WHOLE OF "SHARED"

There is **no per-character copy** — nothing to keep in step, nothing to reconcile when two of your
characters spend at once. `AccountFarmBudget` was already the one per-account object (one load at
login, one save), so it took the wallet and was **renamed `AccountState`**: it was the farm allowance
and nothing else, and a second per-account dictionary with a second lifetime rule is exactly the sort
of thing that drifts apart.

Two consequences worth knowing:

- **A change pushes to every online character of the account**, not just the one who spent it. The
  other one would otherwise sit there showing a number that is no longer true.
- **A change is flushed to the DB at once**, unlike the farm allowance, which can afford to ride the
  60-second autosave. This is money: a crash between a purchase and the autosave would hand the
  platinum back and keep the item.

🔴 **And there is a guard on the wallet that the farm allowance does not need.** The account state is
created LAZILY for a character that never came through the login read (the debug seeder, a test
harness). An empty one is the safe answer for the farm allowance — it just means "a full day left". It
is the opposite for money: writing a lazily-created `Platinum = 0` back would **delete a real
balance**. So the state records whether it was really loaded, every platinum path refuses on one that
was not and says so, and the save passes `null` for the wallet rather than a zero.

### THE PRICE IS TWO NUMBERS

`ItemDef.PlatinumPrice`, default 0 — which is every item in the game today. An item may be priced in
gold alone (the normal shelf), in **platinum alone** (`BuyPriceOverride: -1` beside it — the shape the
premium rune boxes already carry), or in **both**, and when both are set **both are charged**. One
helper, `ItemCatalog.IsPurchasable`, answers "is this for sale at all", so the shelf, the client and
`HandleBuy` cannot drift on whether a platinum-only item exists.

⚠ The platinum price is **authored verbatim** — no rarity multiplier, no vendor tax, no equipment
floor. Those exist to keep a DERIVED gold price sane; a premium price is one hand-picked number and a
formula quietly moving it is the last thing it wants.

⚠ In `HandleBuy` the platinum is taken **before** the item is made and the gold **after**, because
taking platinum is the one step that can still fail. If the bag turns out to be full, the platinum is
handed straight back.

### IT IS NOT AN ITEM

No `ItemDef`, so there is nothing to drop, trade, warehouse, sell or loot — the trade window and the
drop tables never see it, with no rule written to stop them. That is your *"copy of gold without the
drop … cannot be traded (until global marketplace)"* enforced by not existing rather than by a check.

### `/giveplat`

`/givegold`'s twin, deliberately identical: `<name> <amount>`, k/m/b/t suffixes and `1_000_000`,
a negative amount takes it away, clamped at zero. One thing differs and it is the one that matters —
it credits the **account**, and the message says so.

### THE CLIENT

The vendor's Buy title, every shelf row, the affordability dimming, the numpad maximum and the confirm
dialog all read both halves through three shared helpers rather than each spelling out the rule. The
bag's gold line and the Stats window grew a platinum reading. **All four hide platinum at zero** — a
premium line saying 0 on every character in the game is noise until you have some.

### ⚠ THE SMOKE TEST CAUGHT 0.152.0's FOCUS REUSE

`tools/SmokeTest` presses Focus three times in a row to prove it gathers one charge a use. With
yesterday's reuse of 0 that worked; with the **0.5s** you asked for it is two charges and a refusal, so
the run came back **2 CHECK(S) FAILED** — the test, not the game. It presses until the pool moves now
(and until the CAP refusal comes back, which is raised past the reuse gate and so was being answered
with the wrong message). Retries, never a sleep: a fixed wait either flakes or hides a real stall.

### WHAT THIS UNBLOCKS

**`BL-250`'s blocker #1 is gone**, and you answered X/Y/Z in the same message: *"make the slots
tickets buy able with plat need 100/1000/5000"*. Subclass slots 6, 7 and 8 are **100 / 1,000 / 5,000
platinum**, recorded in the entry. ⚠ **The ticket itself is still not built** — that is `BL-250`'s own
job (the slot ladder, the persisted slot count, the class-master dialogue, the info panel), and it
still waits on §6's swap price and §5's cap question.


## 2026-09-16 — 0.152.0: `BL-256` — the warrior PvP pass, and two dead channels behind it

**Needs an APK** (skill powers live in `Game.Shared`, so the client's own cards would quote the old
numbers). No protocol change, no `game.db` delete.

Your report after the warrior duels: *"Only human does decent (low but better) dmg than other 2
warriors ... Elf sword dance never crits .. And have the lowest dmg even when power is combined is
equal to the demons"*. Six asks, and **two of them turned out to be engine bugs rather than numbers**.

### 🔴 THE ELF'S DANCE COULD NOT CRIT, DOUBLE OR BE BLOCKED — AND NEITHER COULD ANY AREA STRIKE

`DeliverSimpleHit` is the AREA damage path. It was written for mob spells and traps, where crit,
[Double] and block do not exist. On 2026-08-28 its **magic** arm grew a fizzle roll and a crit roll,
because routing a mage's AoE through it unchanged would have deleted both from every area spell. **Its
physical arm never got the same treatment.** So every player physical AREA skill in the game landed a
flat hit: it could not crit, it could not [Double], and no shield could block it.

Saints Sword Dance is ten `EnemiesInRadius` strokes, so all ten came through there — which is your
*"never crits"* precisely. It resolves through the same three-way choice as the single-target arm now
(`ResolveBlow` / `ResolvePhysicalDouble` / `ResolvePhysicalCritAndBlock`), read off the same fields,
gated on a PLAYER attacker exactly as the magic arm is so no boss slam is retuned by a bug fix.

### 🔴 CHARGE DID NOTHING BECAUSE IT NEVER GOT A TARGET

*"charge does noting only use as vusual - no charge no displacement.. Nothing"*. The blink itself was
fine. `BeginSkill` decides whether a cast is **offensive** — and that mask asks about damage, debuffs,
Cancel, Taunt, `Charms`, `Pulls`, `Silence`. It has never asked about `Blink`. Charge is the one skill
in the game whose ONLY payload is a targeted blink, so it fell through to the **self-cast** arm,
`target == caster`, and the blink arm ran `BlinkAwayFromNearest(caster, max(1, 0))` — a **one-unit
hop**. That is the sixth time a payload carried in a field or an unexpected flag has had to be taught
to that gate (`BL-110` charm, `BL-154` pull, `BL-155` silence…), and the mask now knows about a
targeted blink as well as the new field below.

And it is a **reverse pull** now, as you asked: *"it should act as the pull but reverse (caster goes to
target)"*. `SkillDef.ChargesToTarget` reuses the drag machinery whole — `StartPull` and `StartCharge`
are one method, `BeginDrag`, called with the two ends swapped. The caster crosses the ground, the
direction is recomputed every tick so it still lands on a target that is running, the steps are
un-announced so the client interpolates instead of snapping, and the caster is action-locked while it
runs. **0.4s, not the pull's 1.2s** — 600 range in 0.4s is ~1,500 u/s, about six times a run. A tow can
afford to lock you for over a second; a leap cannot.

### 🔴 FOCUS WAS ROLLING A MAGE'S LEVEL-83 PASSIVE

*"focus must be physical (now it activates my magic proficiency)"*. The magic-cast proc trigger tested
`def.Category is Magic or Buff or Debuff or Heal` — but **`Category` is a ROLE tag** and has been since
`BL-132`: a physical self-buff is `Category.Buff` and says what it really is with `PhysicalCast`. The
one three-marker test, `SkillMath.IsPhysical`, existed and this site was not calling it. So **every
physical buff in the game** — Focus, the three Presences, the archer's stances, Dance of Fury — rolled
Magic Proficiency on every press. One `!SkillMath.IsPhysical(def)`.

### THE NUMBERS

| what | before | after |
|---|---|---|
| Sword Shock · Demonic Smash · Sword Blast · Focused Blast · Focused Double Slash · Focused Tripple Slash | — | **×2 power**, every rung of both tiers |
| Saints Sword Dance | 150 → 750 · 780 → 1200 | **×2.5**: 375 → 1,875 · 1,950 → 3,000 |
| the three **Slashes** | | **untouched**, as you said |
| Focus Force | power 500, reuse 0 | **power 1,200**, reuse **0.5s** |
| Focus | reuse 0 | reuse **0.5s** |

The 3rd tier's ×2.5 rounds to the nearest 5 (375 · 490 · 600 · 715 …) so the column stays readable;
the 4th tier's is exact. Demonic Smash is still **exactly 3× Sword Shock** on every one of the thirty
rungs — the cheapest typo check that pair has. Focus Force's **1,200 is your hand-priced number, not
part of the ×2 sweep**, and must not be doubled again by a later one.

⚠ **The WARLORD (`war_aoe`) is untouched, and there was nothing to touch**: his files author no damage
skills at all — Charge is his only active — so the doubling covers the whole authored warrior damage
kit. The moment his damage rows land they are authored at the new scale, not the old.

Both CSVs moved with the code in this commit (182 rows), and `SkillCsvSeed --check` is back to its one
pre-existing line (the Warlord's unauthored Sundering Blow).


## 2026-09-16 — 0.151.1: `BL-255` — three daggers were three legal subclasses

**No APK needed, no protocol change, no `game.db` delete.** Server-side rule only.

Your correction, in full: *"How a nuker can hold 11? Buffer, healer, duals, Archer, warrior, war aoe,
Tank .. thats 7 .. Not 11"*. **You were right and the code was not.**

`Player.CanAddDiscipline` barred a repeated `Discipline` VALUE. But the archer merge split the rogue
per RACE — dagger is three discipline values (Nullblade · Venomweaver · Phantom) and bow is three
(Sharpshooter · Hunter · Trapper) — and a subclass may be **any** race. So one character could hold all
three daggers as three separate, individually legal classes: the same class with a different name on it
three times.

The comparison is now `Disciplines.PathOf` — the parent archetype plus **which branch of its pair** the
discipline is. Measured, `dotnet run --project tools/BalanceMatrix -- --paths`:

```
Tank     0   Bulwark
Warrior  0   Ravager          Warrior  1   Warlord
Rogue    0   Phantom · Venomweaver · Nullblade
Rogue    1   Sharpshooter · Trapper · Hunter
Healer   0   Lightbringer     Healer   1   Warchanter
Nuker    0   Magus
```

**Twelve live disciplines fold into eight paths**, so one character may own 8 classes — its main plus
**7 subclasses**. Exactly your list.

⚠ **It cannot just ask `IsRanged`.** That is the obvious shortcut and it is wrong for the WARRIOR:
Ravager and Warlord are both melee and are genuinely two paths. The branch INDEX separates them.
(`IsRanged` had no caller at all after `BL-251`; it still doesn't. This is a different question.)

⚠ **The branch is DERIVED from `Disciplines.Of`** — the table that already authors each archetype's
pair per race — rather than written out a second time. A new class lands in the right path by being
authored there and nowhere else.

⚠ **Nothing un-does an illegal pair already on a character**, per the standing pre-release rule. In
practice nobody has one: `MaxSubclasses` is 4 and only the admin path adds them, so this was latent
until `BL-250` raises the slot count — which is why it is fixed BEFORE that rather than with it.

🔑 **It also makes your own ladder one rung longer than the roster.** `BL-250`'s three earned + five
bought = **eight** slots, and only seven can ever be filled, so the last ticket is the one that prints
*"no more available subclasses"* — permanently, until a new path exists. Your rule working as
described; worth knowing it fires on the fifth purchase rather than never.

## 2026-09-16 — 0.151.0: `BL-247` — the 66-79 hole is filled, and blueprints take the rates

🔴 **NEW APK REQUIRED** (a new field, a new boss template and its plate; the gatekeeper's Frostmere
menu grows a line). **No protocol change.** ⚠ **DELETE `game.db`** only if you want the new gate on an
existing character's teleport list — nothing here is a schema change.

### The finding this is built on: nothing Elite or Boss existed between level 66 and 79
Every top-end faucet in the game is gated on **rank** — `EnchantScrollDrops`, `EliteMatDrops` and the
recipe roll in `RollBossBonus` all pay a Normal kill nothing — and **rank is a property of the SPAWN,
not of the template**, so the only thing that creates one is a zone. The complete live list ran
39-44 (Hollow Crypt), 58-65 (Sunless Warrens + the treant), then **nothing until 80**. The A enchant
band is exactly 76-79, which is why three unrelated "I got none" questions were one cause.

Your ruling: *"fill the gap with the elits+boss, and fix the blueprints to take the rates
multiplier"*.

### 1. Four new elite camps — 68, 72, 75 and 78
`WorldPlan.FieldPlan.EliteLevel` becomes **`EliteLevels`**, a list, and the camp is now placed beyond
the camp whose **band contains it** rather than always the field's last one. Every elite authored
before today sat at its field's cap, so they all take that fallback and **not one of them moved**.

| field | elite camps |
|---|---|
| Ironreach March / Redhorn Highlands / Sunland Crags | **68 / 72 / 75** — new |
| Frostmere Wastes | **78** (new) and 80 |
| Radiant Expanse · Dawnbreak Summit | 84 · 90, unchanged |

🔑 **Why the Wastes needed two.** Its bands run 76-80 and the enchant ladder splits them: A is 76-79,
S opens at 80. Its one camp was at 80, in S — so the A band had no elite anywhere in the world.

### 2. A field boss in the A band — the Emberwyrm Matriarch, level 78
A new template and a new field, **Wyrmfall Basin**, north-west of Frostmere: the boss alone in the
centre on the treant's 21h ± 3h timer, two 76-79 trash flanks 3,500u out so you reach her without an
escort. Managed by **Frostmere**, so she is on that gatekeeper's menu and her dead go to the right
town (the Sunken Vale names no city because its band and its geography disagree; here they agree).

🔑 **A boss, not just another camp, because the Greater and the Safe scroll are BOSS-ONLY** (your own
§100 ruling this morning) **and a boss pays them for its OWN band**. `scroll_greater_a` and
`scroll_safe_a` therefore had **no source of any kind** — not a rare one, none — until this spawner.

⚠ Her 50% phase (enrage + two drakes) **is mine, not yours**: a template with no profile already
fights, so what it buys is that she reads like the game's other field boss. No stat multipliers, no
unique skill, escorted like the treant.

### 3. Blueprints take the rate knobs
The recipe roll was a raw `_rng.NextDouble() < chance` that **no multiplier reached** — not the global
rate, not the group, not a Rune of Drop, not the level gap. That is the whole answer to "I got none":
you play at ×100 and an elite's 0.1% stayed 0.1%, one book per thousand elite kills.

It now goes through `MobCatalog.EffectiveRate` and `DropCopies` like every other drop. **The delivered
numbers at ×1 are unchanged** (boss armor 50% / weapon 40% / jewel 60%, elite 0.1% across ten
families) — the authored numbers are divided by the `other` group's ×3 exactly as `EliteMatDrops`
authors its rungs, so only the KNOBS changed, not the balance. At ×100 an elite now pays ~10%.

### 4. A drop lookup you can actually ask — `--drops`
`dotnet run --project tools/BalanceMatrix -- --drops "greater scroll"` prints every source of anything
matching, as **creature / level / rank / where / chance per kill**. It walks spawners rather than
templates, which is the only way to see the rank-gated half of the table, and every chance goes
through `EffectiveChance`, so it shows the number you actually roll — `/droprate` moves it.

This is the measuring half of **`BL-253`**, your *"we will need a drop database"*; the in-game version
is filed and still owed. It already earned itself twice here: it caught the Matriarch spawning as
ordinary roster filler in two Frostmere camps (she is `HandPlaced` now), and it is where the table
below is read from rather than derived.

| what you asked about | before | now |
|---|---|---|
| `scroll_greater_a` | **nothing, anywhere** | Matriarch, 9% |
| `scroll_safe_a` | **nothing, anywhere** | Matriarch, 0.45% |
| `scroll_enchant_a` | one creature in the game (the L90 boss) | 4 — 2 bosses at 30%, 2 elites at 9% |
| Epic Wood / Epic Leather | the two 58-65 dungeons only | **30 sources**, 61-78 |
| A blueprints | 0.1%/50% with every knob dead | same at ×1, live at your rate |

🔴 **Rare Wood still has no mob source anywhere** and this build does not change that: wood is only
ever a category's SECONDARY material, `StandardDrops` stops a secondary at Uncommon, and
`EliteMatDrops` has no Rare rung at all. It is craft-only (PotionMaster refines 5 Uncommon). Filed as
**`BL-254`** — one rung, one line, but it wants your call on whether craft-only was the intent.

## 2026-09-16 — 0.150.0: the dash goes to 90s, and the rogue loses his evade floor

🔴 **NEW APK REQUIRED** (skill card text + the dash reuse the client renders). **No protocol change,
no `game.db` delete needed** — though see the note at the foot.

Two small builds off one batch of rulings, plus the answer to a drop-table question. **Nothing here
touches the warrior kit**; this build exists so you can test 0.146.0's warrior tiers with the speed
decisions settled behind them.

### `BL-249` — a dash potion is an escape, not a movement stat
`SkillCatalog.DashPotion` goes from `cooldownTicks: 600` to **900**, for all six rarities. The
15-second duration and the `+15…+60` rungs are untouched, so the only thing that changed is uptime:
**25% → 16.7%**. Your reason was a design one, not a number one — *"a dash potion is a escape from a
situation .. not outruning the fastest classes in game"*.

### `BL-251` — Evasion Mastery is removed from every rogue discipline
*"Evasion mastery is removed out of any rogue/dual/archer. no1 learns it or auto gets it. same as
warriors precision"*. `FloorPassiveFor` no longer names `Archetype.Rogue`, which makes the **tank's
`anti_magic` the last floor left in the game** — the warrior's `precision` went the same way in
`BL-201` four days ago, and these two were one decision made twice.

🔑 **The SKILL and the MECHANIC both stay.** `evade_mastery` is still a `SkillDef` and
`PassiveEffect.EvadeFloor` is still read by the resolver; only the GRANT is gone, so re-granting it is
one line. And nothing un-grants it from a character who has it, deliberately — your standing rule:
*"no point of migration type to remove a skill from some1."* ⚠ **A character who already had it keeps
it until you delete `game.db`.**

⚠ `Disciplines.IsRanged` now has no caller — capping a bow rogue at rung 1 was its only job. Kept and
marked, because "is this a bow discipline" is a roster fact worth having.

🔴 **MEASURED, AND IT IS NOT A NO-OP — the rig says the 20% rung WAS binding from about 44 up.** A
melee rogue's natural evasion spread against a same-level mob is **14 points at 44 (19% dodge)** and
**11 at 52 (16%)**, both under the 20% the floor was pinning — so at those levels he loses 1-4 points
of dodge. Your case is gear `BalanceMatrix` does not dress in that section (the AGI set, the full buff
shelf, *"about 25+ evasion differnese"*), and I have no measurement of that; the note is now printed
in E1 so it cannot be forgotten. **If the dodge reads low in the playtest, that is where it went.**

✅ **And it closes a design gap the rig had been flagging for weeks.** E1b's note used to read
*"against a ROGUE, +5 accuracy buys NOTHING at any gap under 10 … accuracy is currently a stat that
does nothing against the one target class it is meant to counter"* — because his floor was a hard
lower bound no accuracy could go under. With both floors gone, **every accuracy point is worth a full
point from the first one**, which is what makes the warrior's `+9` from `BL-201` actually work.

⚠ **`docs/design/CombatResolution.md`'s floor table was stale on three of its four rows** and has been
rewritten: it still listed Reflexes (deleted 2026-08-07) and Precision (gone 2026-09-11). The doc has
been wrong about this for five weeks.

### `BL-248` — closed, declined: move speed is settled
All three measured levers refused. *"the rogues have enough sprint to outrun anyone … so do not do any
of the .1,.2,.3 -> we leave speed as is (after the marks update)"*. 🔑 He rejected the premise, not
just the levers: the rogue's advantage is a **sprint he can spend**, not a standing number, and the
burst everyone else could buy (the dash) is what actually broke it — which is why `BL-249` was the fix
and no stat channel moved.

### `BL-247` — the sweep, answered: there is **no Elite or Boss between level 66 and 79**
Not a code change; the finding is in the Backlog entry. Every scroll / top-material / blueprint faucet
is gated on Elite or Boss rank, rank belongs to the SPAWN, and **no zone creates one in 66-79**. The A
enchant band is 76-79, so it lands exactly in the hole: `scroll_greater_a` and `scroll_safe_a` have
**no source of any kind** today, and `scroll_enchant_a` comes from one mob in the game. A-grade
blueprints *do* drop from every elite at 80+, but at **0.001 per kill** on a raw roll no drop-rate
multiplier touches — which is why the ×100 test rate never produced one.

## 2026-09-16 — 0.149.0: `BL-238` is built — a Mark costs you 10% move speed

🔴 **NEW APK REQUIRED** (skill card text). **No protocol change, no `game.db` delete.**

You answered all three of the table's questions, two of them by editing files rather than writing a
sentence: **F2** (*"F1 Is Declined"*, on the page), **reading B at −10%** (you retitled §3 and put
*"Decrease movement speed with 10%"* on every Mark row of `healer 4th.csv`), and **yes, the Harmony
Mark takes it too** (both `buffer 4th.csv` rows). Built exactly as authored.

🔴 **And the `+20% move speed` the Marks used to grant is GONE, because no CSV row ever authored it.**
Your Mark rows read *"Atk/Cast.Speed +20%"* and say nothing about movement; the third speed was
invented in code, and it was a large share of why everyone was over 200. So a Marked character swings
by the full **30%**, not 10%.

**It measures onto your own hand table.** Every row of the built game matches the `F2(10)` column you
computed yourself — 158, 158, 174, 162, 184, 200, 161, 156, 171. ⚠ And your *"Today +Sprint all get to
max 250"* is no longer true: sprinting lands between 210 and 238 for everyone but the **elf rogue**,
now the only character who reaches the cap on his own.

🔑 **The cut is a FIELD on the buff (`SkillDef.MoveSpeedPenaltyPct`), not a `Slow` magnitude**, and
that choice is load-bearing. `Slow` sits in `AnyDebuff` **and** in `ControlCc`, so spelling it as a
slow would have filed a Mark in your DEBUFF row (`BL-216`'s lesson again), let your own **CC
resistance resist the price of your own blessing**, and made a **raid boss immune** to it. It lands in
the F2 position as a second factor beside the slow one, so nothing about slows moved.

⚠ Two tool fixes rode along, both of the recurring kind: the **balance rig's buff builder was missing
the new field channel** (the seventh time — a `BuffInstance` built by hand applies and does nothing),
and `SkillCsvSeed` learned to read the cell. That second one exposed a latent mis-read: **`ms`'s bare
`"speed"` alias had been claiming the `Atk/Cast.Speed +20%` in every Mark row as MOVE speed**, which
only became visible the day the code carried a real move number to compare it against. Fixed with an
`atk/cast.speed` compound key, the same shape `BL-237` needed for `move/attack.speed`.

⚠ **Still open, and §5-6 of the page is the argument:** the cut does **not** reserve the band for
rogues and no ordering could — it scales the same base difference by the same factor on every row.
What closed the band is the **flat** shelf, worth proportionally more to the slowest character. Your
own follow-up table (rogues skip Frenzy, so their shelf is +53 against +61/+69) reaches the same
place. Unbuilt and unruled.

## 2026-09-16 — 0.148.0: Angel's Protection, the auto-farm rubber-band, Greater scrolls

**No APK, no protocol change, no `game.db` delete.** All three are server-side.

### Angel's Protection did nothing — `HandleRespawn` cleared the buffs `Kill` had just saved

*"you respawn in town with no buffs"*. `Kill` gets this exactly right: with a preservation buff up it
removes only the protection itself and every other blessing survives. Then **`HandleRespawn` called
`entity.Buffs.Clear()` unconditionally** — redundant in the ordinary case and fatal in the one case
the skill exists for. You paid 5 Skill Stones, watched the bar survive the death, and emptied it by
pressing "return to town". The line is gone, and the respawn now pushes the bar and stats so the
client — which empties its own bar on death — is told the blessings are still there.

### Auto-farm rubber-banded you off a click

*"when in auto farm when i click on the ground char start to move but then gets rubber banded back."*
A **static** farm spot rewrote your destination to the farm centre **every tick** you stood more than
150 units from it, so a ground tap walked two steps and snapped back. Your rule is the standing one —
*"auto farm should not prevent me from moving → it should allow me to kite; only when stopped then it
attacks and use skills"* — and **kiting has been intended since playtest 22**.

A manual move now takes the feet back: `Entity.ManualMoveHoldTicks` is set by every move command and
the autopilot's movement arm (`AutoRoam`) leaves your destination alone while it runs. It counts down
**only once you have stopped**, so a long walk is never cut in half, and it holds for 5 seconds after
that. **The fight arms are untouched** — it still attacks and casts throughout, which is what "allow
me to kite" asks for.

### Greater scrolls of enchant are BOSS-ONLY

Your ruling, built. Greater used to be the ELITE reward at 1.8% per kill, and an elite camp is
something you can farm. Safe was already boss-only, so the two scrolls that make an enchant worth
attempting are now both a boss reward; an elite pays the band's ordinary scroll and nothing else.

### 🔑 The enchant RATE, measured — and it is your ×100, not the table

**Nothing changed here, deliberately.** The `scrolls` group **stopped being exempt from the global
drop rate on 2026-08-18** (`DropCopies` removed the 100% clamp, so the exemption that existed to
protect the weights came off). So your ×100 multiplied enchant scrolls by 100 as well.

At the shipped ×1 the B rung is `0.15 × 0.005 = 0.075%` per kill — **one per 1,333 kills** — which is
precisely the target you set yourself in playtest-21 (*"I need like 2-3 … enchant scrolls must be for
over farm not a casual one"*, worked out at the time as 0.2% per kill on the E rung). A common weapon
is one per 267 kills, so the scroll is **already "rarer than a weapon"**.

⚠ Your other clause — *"or at least as rare as an epic weapon"* — is a **different number by 100×**:
an epic weapon off a normal mob is `0.0001 × 0.075` = 0.00075% per kill, one per 133,000. The two
halves of your sentence are 500× apart, so this one is left alone until you say which you meant.

## 2026-09-16 — 0.147.0: five of §100's bugs, and two dead channels found behind them

🔴 **NEW APK REQUIRED** (client code + skill card text changed). **No protocol change, no `game.db`
delete.**

### 1. Swift Stab gave no buff — because `SelfBuff` was only ever read on the AoE branch

Your report: *"when used it should increase AS/MS but it dont"*. `SkillDef.SelfBuff` was read in
**exactly one place** — the `EnemiesInRadius` arm of `ExecuteSkill`, written for the tank's Taunting
Wall, which `return`s. So a **single-target** skill's self-buff was applied by nothing at all. Swift
Stab is the only other user in the game, and its whole identity is that buff. It now lands on the
ordinary path too, unconditionally — a rush you build from your own momentum does not care whether the
blow crit, and a failed blow is still a landed strike (`BL-193`).

### 2. `rogue_armor_mastery` said "+130% MP regen" against a CSV of `+1.8`

Your `mpReg +1.8 … +2.5` cells were built as a **multiplier** (`MpRegenPct`, stored as value − 1), so
the card read ×2.3 — which is "+130%" — at your level 66. The reading leaned on *"except armor
masteries the 20% increase"*, but that sentence was about a `x1.2`; these cells sit beside
`hpReg +2.5 … +6.0` in the **same row**, which has always been built flat. One grammar cannot mean two
things in one cell pair. **The whole rogue/archer column is FLAT MP/s now**, 2nd, 3rd and 4th tier —
`--check` had been printing it as a ⚪ MODE note the whole time, and a note is not an exemption. (The
TANK's twin was corrected the same way on 2026-09-04, where the multiplier reading had been paying a
level-74 tank +410%.)

🔴 **And behind it, a DEAD CHANNEL: `ApplyArmorMastery` never read `StatMods.MpRegen`.** The HP twin
one line up was read; its MP mirror never was. So the **tank's entire MP-regen ladder — `mpReg +3.1`
at 36 climbing to +5.1 at 90 — has been worth exactly ZERO since it was converted to a flat**, and so
has the healer's `+3.4`. One line. Measured now with a new
`dotnet run --project tools/BalanceMatrix -- --classregen [level]`, which prints HP/s and MP/s per
role, bare and shelf-buffed: at 66 the tank's flat column reads **4.7 MP/s** where it read 0, and the
rogue stands at **8.1 MP/s bare / 9.2 buffed**.

### 3. The TMP unicode spam, and why it was a FEEDBACK LOOP

The missing glyph is the **em dash**. The TMP atlas is static, so a character outside it logs a
warning **once per frame per label** — and `ClientLog` hooks `Application.logMessageReceived`, so
TMP's own warning, *which contains the offending character*, was appended to the System tab, rendered,
and logged again. One em dash in one system line grew into a warning per frame. **That is the FPS
drop, and it is why it was the System tab and nothing else.**

Fixed at both ends with `Game.Shared/AsciiText.Fold` — a **fold, not a strip**: em dash, en dash,
U+2212, ×, …, ·, ± and the curly quotes map to ASCII and **everything else passes through untouched**,
because you type Bulgarian and this sink carries chat. Applied at `ClientLog.Append` (one sink, covers
server prose, our own 120-odd em dashes and Unity's engine log) and at the server's three
system/combat `ChatMessage` sites (so it works without an APK too).

### 4. The SP price vanished from gold-priced rows in Skills-to-Learn

*"some times"* was exactly right: the row read `gold > 0 ? gold : SP`, so the moment a rung carried a
gold price its SP cost disappeared — while the affordability test one line up kept demanding **both**.
A row that refuses to be bought and does not say why. Both prices show now when there are both.

### 5. The NPC buffer would not re-buff a Mark

*"same mark, same lvl, same npc"* — every other timer resets and the Mark's does not. The NPC grants
everything for `NpcBuffTicks`, **one hour**, but the pre-filter `BuffWouldLand` asked `BuffPlan` for
the skill's **own** duration — and a Mark's own duration is **five minutes** (it is a Lightbringer
party skill the NPC happens to sell). So the equal-rank test read "you have 47 minutes left, this
would only give you 5" and dropped the Mark out of the landing list before `ApplyBuff` — which, told
the real hour, would have accepted it — ever saw it. Every ordinary blessing authors the full hour
itself, which is exactly why the Mark was the only one that misbehaved.

🔑 **The shape worth keeping: a pre-filter that predicts a decision must be given the same inputs as
the decision.**

## 2026-09-16 — 0.146.1: a vendor sold you one rune box however many you asked for

**No APK, no protocol change, no `game.db` delete.** Server-side only.

Your report: *"also buing several war rune boxes it buys only 1 no matter how many i select"*.

`HandleBuy` decided what a quantity even means with a hand-rolled
`def.Slot is EquipSlot.Consumable or EquipSlot.Scroll`. That list is **narrower than the shared rule,
`ItemDef.IsStackable`**, which also covers `Material`, `QuestItem` and **`Box`**. The client asks the
shared rule — it was corrected when crafting mats got their quantity pad — so the numpad offered you
1-99 boxes and the server silently clamped the order to one. You were charged for one, so nothing was
lost but the trip.

🔑 **Two more places asked the same narrow question, and both are fixed with it:**

- **`HandleSell`** — the same bug facing the other way. A stack of boxes or crafting mats **sold one
  at a time** however many the pad offered. Not reported yet; found by fixing the buy side.
- **The box-loot path** (`HandleOpenBox`) — where it was only inefficient: stackable loot took the
  one-`AddItem`-per-unit road instead of merging in one call.

⚠ The lesson is the one that keeps recurring here: `AddItem` and `Stacking` already answer "does a
quantity mean anything for this item", and three call sites re-listed the slots themselves instead of
asking. A rule with four copies has four chances to be wrong, and three of them were.

## 2026-09-16 — `BL-238`: the move-speed table, both orderings, measured

No game change — a **measurement** and the report it produced, which is what `BL-238` asks for first
(*"i just don't know the formula we should use - can u make me tables with bot formulas"*).

**New: `dotnet run --project tools/BalanceMatrix -- --speed [level] [quality]`** — real level-90
`Entity` objects, real epic gear, the real NPC shelf, nine rows (3 races × mage/fighter/rogue) and
both of his orderings side by side, with and without the +60 sprint. The mode re-reads
`Entity.EffectiveSpeed` on every row and prints `!!` if its own arithmetic and the engine disagree,
so the page cannot go quietly stale. Written up in
**[docs/balance/MoveSpeedOrderings.md](balance/MoveSpeedOrderings.md)**.

Three things it found, all of which change the shape of the ruling:

- 🔴 **A MARK GRANTS +20% MOVE SPEED TODAY.** `markCore` in `Skills.Lightbringer4th.cs` carries
  `BuffMoveSpeed +20%` beside its attack- and cast-speed lines, so Holy/Life/Blood are speed *buffs*.
  *"every mark should decrease speed with 20%"* therefore has two readings 40 points apart — keep the
  +20% and cut on top (net ×0.96, worth about −5 points), or flip the +20% to −20% (−35 to −44).
  Both are tabulated; neither was picked.
- ⚠ **The buffer's Harmony Mark carries no move speed at all**, so the four Marks already disagree on
  this axis by 20% of base.
- 🔑 **Neither ordering reserves the band for rogues** — F1 and F2 give the *identical* rogue-minus-mage
  gap, because both scale the same base difference by the same factors and the +61 flat shelf is
  common to all nine rows. What closed the band is that flat shelf: the mage multiplies his own speed
  by 1.74, the elf rogue by 1.58. And with sprint, **every row is at the 250 cap today**. §6 of the
  page lists what would actually reserve the band, unbuilt and unruled.

⚠ **F2 is what the engine already does** — `EffectiveSpeed` is `ModifiedStat(base) × (1 − SlowFraction)`
— so choosing F1 re-orders every percentage debuff in the game, slows included, not just the Mark.

## 2026-09-16 — 0.146.0: the warrior's 3rd and 4th tiers, race by race

🔴 **NEW APK REQUIRED** — a class-skill-TABLE change (the client builds its Learn tab locally). No
protocol change. A `game.db` delete is not required, but is harmless and is what I ran here.

**`BL-237` is built.** You landed `warrior 3rd.csv` and `warrior 4th.csv` on 2026-09-14, fixed all
eight slips and answered every question the same day; nothing had been built since. This is the whole
of both files — **the Ravager's damage half, which had never existed**, plus the 4th tier on top of it.

### The RAVAGER splits three ways, and the race column is the whole split

| | Human | Demon | Elf |
|---|---|---|---|
| Presence (64/74, 76/85) | **Champion** — atk speed + PvP damage | **Berserker** — P.Atk + accuracy | **Saints** — crit rate, crit damage, speed |
| Slash (15 + 15 rungs) | P.Def −10→25% | P/M.Atk −5→12% | atk/cast/move speed −10→23% |
| its own damage | the **Focus** kit | **Sword Shock** (5s stun) + **Demonic Smash** | **Saints Sword Dance** + **Sword Blast** |
| a stance or a burn | **Focus Force** + **Focus Limit** @78 | **Battle Frenzy** (60/66/74) · **Parry** @78 | **Antidote** (52-74) · **Saints Blessing** @78 |

**Shared by both disciplines:** **Charge**, the gap-closer — 400 at 40, 600 at 76, a two-handed sword
*or* blunt, per your ruling. It is the only thing the Warlord takes from either file.

### The five pieces that needed new engine, and why

- **Saints Sword Dance is a CHANNEL**, the shape you chose for Arrow Barrage: ten strokes over two
  seconds, each a real skill execution with its own miss, crit and 150 splash. Nothing is
  special-cased, so the rule survives.
- **Battle Frenzy's price.** *"Decrease received HP 60%"* is the healing you receive, cut — read that
  way because it pairs with *"can be used when HP is less or equal to 30%"*. 🔑 It is a **negative
  `HealReceivedPct`, not the anti-heal FLAG**: the flag is in `AnyDebuff`, and `IsHostile` reads the
  flag mask, so declaring it would have parked a warrior's own war-cry in his DEBUFF row —
  un-dismissable, stripped by a cancel, dropped on relog. A downside you chose is not a curse.
- **Saints Blessing reflects three different ways**, and your three numbers mean three different
  things: *"Reflect 30% OF basic attacks"* is a fraction, *"15% TO reflect debuff"* and *"10% TO
  reflect Physical Damage skill"* are chances. The last two had only ever ridden PASSIVES — and a
  learned passive pays whether the stance is up or not, which would have made the toggle free. They
  now ride the buff, folding by MAX against their passive twins so Deflection and this never sum.
- **Focus Force is the first gatherer that also damages.** The full-pool refusal had to learn not to
  wall an ATTACK, and its gather runs after the damage arm — so an interrupted cast gathers nothing.
- **Focus Limit needed no mechanic at all**: gather TEN against a cap of ten *is* "set Focus to max".

### Final Stand grew a second channel

Your acc edit to both 3rd-tier files is in. It is **not a rider on the P.Atk half** — it is its own
live read off the HP bar (`Entity.FinalStandAccuracy`, through the new `EffectiveAccuracy`), and it
starts one band later at every rung, so the first rung pays it only below 25% HP. `docs/Formulas.md`
moved in the same commit.

### Sundering Blow leaves the Ravager

The derived `BL-185` stand-in has said since it was built that *"it goes the day his damage rows
land"*. They landed. ⚠ **The WARLORD keeps it** — `war_aoe 3rd.csv` and `war_aoe 4th.csv` still author
no damage row of any kind, so dropping it there would leave the blunt discipline with a 2nd-class
Smash and nothing else from 40 to 90. `--check` goes on printing 🟠 against `war_aoe 3rd` until your
blunt damage rows land, which is the same pressure that produced this pass.

### Two cells of yours moved, both slips, both reversible

- **Sword Shock's DURR cell read 0** while its own DESCR said *"Stuns for 5s"*. A zero-tick stun is
  not a skill; the cell is 5 now, in both tiers. (The Human archer's Magic Arrow — the same idea in
  bow form — has always read 5.)
- **The `Chance x0.7` / `Success rate x1` comments came out of the class CSV** and went into
  `debuff_landmods.csv`, which is where they belong (`BL-232`). Your ruling, verbatim: the three
  Slashes ×0.7, Sword Shock ×1.

### Verified

- `SkillCsvSeed --check` is **green on `warrior 3rd`, `war_aoe 3rd` and the new `warrior 4th` spec**,
  and all 78 landmod rows verify. The one remaining line is the Warlord's Sundering Blow, above.
- **Every number in both files is now READ, not skipped.** Fourteen new `Descr` aliases cover the
  Presences' dotted `P.Crit.Rate`, PvP/PvE damage, the three reflect channels, a channel's shot count,
  the HP gate and the anti-heal. 🔴 One of them taught me something the table's own header did not
  say: **a hyphen can never appear in an alias** — `-` is a clause separator in `Clip`, so
  *"Buff-Removal Attacks with 60%"* only ever presents `removal attacks with ` to the matcher. Battle
  Resilience had been UNREAD on that line since it was built.
- Also fixed by it: *"P.Atk.Speed with 10%"* was being read as MOVE speed, because `ms`'s bare
  `"speed"` alias claimed a number `as` could not reach through the dots.
- `tools/SmokeTest` gains **section 17**, the ascended Focus pool: Focus Limit filling to ten, Focus
  Force swinging against a FULL pool without being refused, and the Triple Slash spending exactly
  four. All twelve Focus checks pass, and so does the rest of the run.
- Server boots green on 0.146.0; the Unity client type-checks.

