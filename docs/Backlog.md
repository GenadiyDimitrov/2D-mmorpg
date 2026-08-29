# Backlog — everything asked for and not yet built

**One list. Features and changes only.** Bugs, verifications and "does this work" live in
[testing/Open-Checklist.md](testing/Open-Checklist.md) during a pass and in
[testing/Playtest-Archive.md](testing/Playtest-Archive.md) after it. This file is the other half:
the things you asked to be **built** or **changed** that are still owed.

Assembled 2026-08-12 from playtests 4-21, `Open-Checklist.md`, `Playtest-Archive.md`,
`Roadmap.md` / `RoadmapNext.md` and the design docs. Everything shipped up to `ed75bac`
(0.60.1 + the playtest-21 batch) has been checked out of it.

**Playtest 22 (2026-08-13) added `BL-65` … `BL-72`** — dungeon level bands, an item-id reference,
the `MpHeal` type, more 16-40 zones, invisibility ×3, mob social clans, the aggro/taunt model, and
unbuffed farm survivability. His bug finds from the same pass went to `testing/Open-Checklist.md`,
not here.

**2026-08-14: he ruled on ALL EIGHT remaining 🔴 items in one message, and all eight are BUILT**
(0.66.0) — `BL-20` · `BL-22` · `BL-27` · `BL-34` · `BL-35` · `BL-36` · `BL-42` · `BL-59`. Two of them
left something behind that is his to answer, and both are flagged on their own entries below:
**`BL-22`'s farm budget cannot be reached at S** by any tuning, and **`BL-34`'s 76+ buff expansion**
was not re-ruled.

⚠ That message covered the eight he was SHOWN, not every 🔴 in the file. Five were still ready to
build and simply unqueued; he ruled on four of them on **2026-08-14**, and **three shipped as 0.67.0** —
`BL-43` (target cycling, retaliate-first) and `BL-46` (treat the app as a game; the second icon is
gone) are **built and deleted**, and `BL-49`'s boss-EXP half was built while its levelling-curve half
stayed open — until **2026-08-26, when he closed it with *"leave it"*** (see the bottom of this file).
`BL-45` (the presentation pass) is **his own "separate discussion later on"**, and its VISUAL half is
now `BL-93`, which he asked to talk about on the same day.

**⏸ CRAFTING IS PARKED, on his instruction (2026-08-14):** *"leave the salvage/mats etc craft until
I'm able to test it fully — need to increase the drop rate and exp by 100 so I can make chars
different professions to farm to see who can craft what — and it's a single playtest only for this."*
So `BL-05` (the two unruled crafting pieces), `BL-22`'s unreachable S budget and `BL-50` (the boss mat
pile vs the party loot rule) are **not to be worked on or re-raised** until he opens that playtest.
Nothing about them is blocked or broken — they are waiting on a test only he can run.

**Playtest 23 (2026-08-15) added `BL-73` and `BL-74`** — mob social clans back on once the world map
spreads the camps out, and the Game-Launcher research. Everything else he found that pass was either a
**bug** (they went to `testing/Open-Checklist.md` and are built as 0.68.0) or a **ruling on something
already built**, which by rule 1 rewrote the thing in place rather than opening an entry here.

**2026-08-15, after that pass, added `BL-76`** — boss skill gems in three rarities. New design, not a
playtest find; queued 🔴 with its numbers explicitly marked as yours to alter later.

**Playtest 24 (2026-08-16) added `BL-77`** — the PvP flag as the input to every AOE and no-damage skill's
target filter — and it was **BUILT the same day in 0.69.0**, together with both of its bug finds (reflect
flagging the defender; the System chat tab lagging), which live in `testing/Open-Checklist.md` §87. The
pass also **answered `BL-47`/`G3` §8-B**: *migrate*, and it named three levers the design doc never swept
(enchant, race as the main-stat carrier, a ×2 elite passive) — see that entry, whose step 2 is the only
thing playtest 24 produced that is not yet built.

**Playtest 25 (2026-08-16/17) added `BL-78` … `BL-83`** — the mob HP curve and the IG comparison he asked
for, two brand-new uses for player-built mobs (**town guards** and **fortress sieges**, both his own
design), god-mode debuff immunity plus the boss debuff rule beside it, the admin/stealth visibility flag,
and **taunt removed from the auto chain**. It also **answered `BL-13`** — a boss is **10-30 minutes**, so
the target rises rather than the late bosses coming down — and delivered the verdict on `BL-47` step 2:
the machinery works and the rune is indistinguishable from the passive, **but the design as it stands
loses the global curve lever**. Every built row in that pass came back green; the UI polish it asked for
lived in `testing/Open-Checklist.md` §89 as `BL-88`.

**✅ ALL SIX OF THOSE ARE NOW CLOSED.** `BL-78`'s two halves shipped in 0.73.0; `BL-82` in 0.80.0; and
**`BL-13`, `BL-81`, `BL-83` and `BL-88` were built together as 0.89.0** on 2026-08-26, the same day he
answered `BL-47` (*yes — field mobs stay on the curve, player mobs are a hand-placed content tool*).
What is left of that pass is `BL-79` and `BL-80`, which are the CONTENT his `BL-47` answer unblocks.

**🆕 2026-08-27: he ruled on FOURTEEN entries in one message, and NINE of them closed.** Deleted:
`BL-10` · `BL-12` · `BL-16` · `BL-17` · `BL-24` · `BL-54` · `BL-55` · `BL-86` · `BL-94`. Rewritten:
`BL-15` (learnable passives, gated on the warrior/rogue CSVs) and `BL-23` (an assertion replaced by a
measurement — `--goldflow`). Partly closed: `BL-90`, `BL-91` and `BL-92`'s demon-buffer bullet. All the
replaced text is in [BacklogArchive.md](BacklogArchive.md).

⚠ 🔑 **THREE OF THE FOURTEEN WERE STALE, NOT OPEN** — `BL-90`'s bursts, `BL-91`'s ×2 and `BL-92`'s demon
buffer were all already in the code, two of them since 0.87.0 three days earlier, and he was the one
who noticed (*"bl-24 - it build ? why blue ?"*, *"nuker 3rd is build or atelast should be so fix the
wording"*). **When a build closes a dependency, sweep every entry that named it in the same commit.**
A stale 🔴 costs more than a missing one: it invites work that is already done.

## The rules this file runs on

1. **Newest ruling wins, and it is the ONLY one shown.** When you re-spec something, its entry is
   rewritten in place. The old text is cut and pasted into
   [BacklogArchive.md](BacklogArchive.md) under the same id, dated. Never two live versions.
2. **An id is permanent.** `BL-07` means the same thing forever, even after a rewrite, so a note
   anywhere in the repo that cites it stays true. Ids are never reused and never renumbered.
3. **Built = deleted from here.** It goes to `CHANGELOG.md`, not into a done-pile in this file.
4. These ids do **not** collide with your checklist ids (`63l`, `C4`, `M9`, `G3`) — those are a
   playtest's numbering and die with the pass. Where an entry came from one, it says so.

**Status marks:** 🔴 ready to build · 🟡 gated on another entry here · 🔵 waiting on you (a
decision, a CSV, a measurement) · ⏸ you put it on hold.

---

## ★ The ones you have named most recently

Three of the original five are **built and deleted** (2026-08-12): `BL-01` the premium reward runes,
`BL-03` the Stat-Swap tab and `BL-04` the auto buff potion/scroll tab — the last two took `BL-39`
(the Mindwriter's misleading `(cost …)`) out with them. See `CHANGELOG.md`. Two are left, and one is
brand new.

**🆕 2026-08-26, in one message:** `BL-47` answered **yes** and closed · `BL-49` ruled *"leave it"* and
closed · **`BL-93` opened** for the in-game visuals discussion you asked for (*"models/terain etc."*)
· and `BL-13` + `BL-81` + `BL-83` + `BL-88` were built as **0.89.0**, so they are gone from this file.

- `BL-93` 🔵 **In-game visuals — models, terrain, the look of the world.** Your next conversation, by
  your own instruction. Full entry under **UI & client**, below.

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier)** — ✅ **FOUR OF THE AUTHORED FILES ARE DONE.** The
  **Lightbringer (healer) shipped in 0.74.0**, the **whole Warchanter (buffer) in 0.76.0**, the
  **Lightbringer's 4th tier in 0.85.0** (with the shared kit and the eighteen Sigils), and the
  **NUKER's 3rd tier in 0.87.0** — 208 rows, 21 families, Magus and Tempest, all three races, 40 to 74.
  `SkillCsvSeed --check` is green on all twelve walked files. That is the proof the pipeline works end
  to end, four times over.

  ⚠ **The nuker one is the lesson worth keeping: `nuker 3rd.csv` had been FINISHED since before the
  healer's was, and nobody noticed for six days** — it was never added to `Check.Specs`, so the one tool
  that would have shouted about it never opened the file. **A finished file that no spec walks is
  invisible.** When you finish a file, say so, and its `Check.Specs` line goes in the same day.

  What is left, and it is now a short list:
  - 🔵 **`buffer 4th.csv` — you are authoring it.** Rows through the Mark block are done; line 125 is
    your `NOT DONE FOR NOW` banner and the bow/blunt/2H masteries, Twin Arrow, Sound Smash and Acoustic
    Shock sit below it. Not started, on your instruction (2026-08-26: *"dont do buffer 4th as im
    authoring it"*). When it lands, Harmony Mark's id is `harmony_mark` and it **must share `MarkKey`**
    with the healer's three or a healer's Mark and a buffer's would stack.
  - 🔵 **Five 3rd files are still two-line placeholders** — `tank` (one real row), `warrior`, `war_aoe`,
    `dual`, `archer` — and **seven 4th files** with them. Same rule: nothing invented in the meantime.
  - ✅ ~~**Calm Spirit**~~ — SHIPPED with `BL-92` in 0.88.0, the moment the MP-regen question it was
    held behind was answered. Nothing of the nuker's file is outstanding.

- `BL-05` 🔵 **Crafting — the two pieces you did NOT rule.** The system itself SHIPPED in 0.63.0
  (masters, six levels, the freeze, the grade ladder, the gear roll, the mat costs, quitting). What is
  still owed is only what you left open:
  - **Where elemental + skill stones sit on the Potion Master's ladder** — *"somwhere and elemental
    stones + skill stones"*, no rung named. Not invented.
  - **The chest / rune-box / exp-box economy**, your own *"something like that"*: both consumable
    masters craft treasure chests of random scroll/potion loot as a sink against the **60kk gap to a
    Mythic S item**; Potion Master → tradable temporary War/Spell rune boxes (1h/2h), Scribe →
    tradable temporary EXP/SP boxes (5-30%, 1h/2h). A sketch, deliberately not built — spec it against
    the held War/Spell Rune and the `BL-01` premium runes, not as a new system.
  - ⏸ **Two numbers, left as they ship (your call, 2026-08-13):** *"the farm times will work on them
    leave them as is .. later will decide on them."* Both are measured and both are odd — the **C rung
    costs 8 Rare mats**, so a C recipe reads cheaper than an E one (the Rare faucet is 0.09/kill against
    Common's 1.76 while your C target is 5-10h), and a **fully S-geared character is 347 farm hours**.
    Shipped as-is on purpose; nothing is retuned until you say so. See `docs/balance/CraftingMats.md` §8.

---

## Combat & stats

- `BL-06` ✅ **BUILT 2026-08-14 (0.65.0)** — a physical skill is no longer subject to the
  accuracy-vs-evasion roll at all; the caster's accuracy, `Precision` and `EvadeFloor` were all
  removed from that branch and now govern basic attacks only. The **only** thing that dodges a skill
  is `Entity.SkillEvadeChance`, and **Evasion Boost is its only source: 25%**. That also settles the
  CSV's *"skill evasion x1.25"* — it was the 25%, not a multiplier. See `CHANGELOG.md`. Delete at the
  next sweep.
  - 🔵 **The 40% rung is NOT built and needs you.** `rogue 2nd.csv` gives Evasion Boost a single
    level, so there is no rung to hang it on and inventing one re-specs your CSV. Same for *"76lvl the
    physical phantom gets a 90% for 15s"* — a 4th-class skill. Both wait on `BL-02`.

- `BL-07` ✅ **BUILT 2026-08-14 (0.65.0)** — `Deflection`, the warrior passive, your numbers exactly:
  **@40 → 0.15 chance ×1.0 reflected, @76 → 0.30 ×1.0**. Auto-granted at the class change like the
  identity floors, on its own 40/76 ladder. Reflected damage is the full hit and can kill the caster;
  a bounce is applied directly so it never bounces twice. Kept separate from the armor sets' basic-
  attack `Reflect`. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-08` ✅ **BUILT 2026-08-14 (0.65.0)** — `Backlash`, the tank passive: **30% chance a debuff lands
  on its caster instead**, on both debuff paths (contested CC and the fizzle model). Rolled BEFORE the
  land contest, because a bounce is not a resist. See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ **One thing is mine, not yours: the LEVEL.** You gave the 30% and never said when a tank gets
    it. It is granted at the **3rd class change (40)**, beside Deflection. If you want it at the 2nd
    (20), say so — it is one line.

- `BL-09` 🔵 **A floor under the wrong-weapon magic penalty, bought back by Spellcaster Mastery.**
  ⚠ **Re-marked 🔵 on 2026-08-14 — it contradicts your own CSV.** This asks for five Mastery rungs
  walking the penalty 0.5 → 0.05; `docs/data/classes_skills_csv/mage 1st.csv` authors Spellcaster
  Mastery as a **single-level, auto-granted, never-replaced** passive carrying the whole rule
  (*"Bow/Dagger/None: cast x0.5, mAtk x0.5, mAcc x0.5"*), and the code matches it exactly
  (`Entity.cs:2264-2282`, `StatCaps.UntrainedWeaponMagicFailMod = 25`). Adding rungs re-specs the CSV.
  Your original words are kept below — say whether the CSV or this note wins.
  *"hitting above the 0 difference is not failing … if we can make a floor … a strong 50% with wrong
  weapon celing((formula),0.5) that is always 50% on the norm … L1 - 0.5 .. L5 ..0.05(the min)."*
  Read as: a wrong-weapon caster is capped at 50% success at parity, and the five Mastery rungs walk
  the penalty 0.5 → 0.05. *(playtest-21 `64c`, never answered.)*

- `BL-11` ✅ **BUILT 2026-08-14 (0.65.0)** — the mob layer gains an **mRes channel**
  (`MobMod.MagicResist` + a *Magic Resistance* mastery track, the CSV's own "???? Resistance" row
  filled in), and the pair is actually authored: **Warded** (P.Def ×0.8 / M.Def ×1.5 / mRes +20%) on
  Grave Lich, Aether Wisp and Spiteful Ghost; **Ironhide** (P.Def ×1.5 / M.Def ×0.8 / mRes **−20%**,
  a real magic WEAKNESS) on Shield Skeleton, Fomor Brute and Dread Knight, plus a Magic Resistance
  rung on Obsidian Knight's Stoneplate. Before this, one mob in the game was anti-magic and none was
  anti-physical. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-14` ✅ **BUILT 2026-08-14 (0.65.0)** — two of your three clauses were already true (a mob's
  attack SPEED and CRIT RATE have come off `InnateWeaponType` since 2026-08-10); the third was not.
  `MobWeaponPowerFactor` (`433 / weaponBaseSpeed`) gives a mob the per-hit power a player gets free
  from the weapon ITEM, so a slow weapon buys damage instead of being a pure nerf. Measured at 40:
  Dual 171 P.Atk / 13.2% crit, Blunt 195 / 4.4%, 2H 227 / 8.8% — and **DPS is flat across all of
  them**, which is what makes it a trade. ⚠ **BOW is ×1.00 on purpose**: `MobRole.Archer` already pays
  that trade explicitly (P.Atk ×2, 450 range), and doubling it would put an archer at ~3× per arrow.
  See `CHANGELOG.md`. Delete at the next sweep.

- `BL-15` 🟡 **`precision` / `anti_magic` should be LEARNABLE PASSIVES, not auto-granted floors —
  and it waits for the warrior/rogue CSVs.** Your ruling, 2026-08-27: *"i would like them to be a
  learnable passive not a auto learn.. so remind me once i start authoring warrior/rogues."*
  - **What changes.** Today both are auto-granted floors: they appear at a level with no row, no SP
    price and no place in a ladder (`--check` reports them as ⚪ AUTO-GRANTED against `warrior 2nd.csv`
    and `tank 2nd.csv`, which is what an auto-grant looks like on purpose). As learnable passives they
    become ordinary CSV rows — learn level, SP, rungs — and the Learn tab shows them.
  - 🟡 **Gated, deliberately.** A learnable passive is a **CSV row**, and inventing one re-specs the
    file you have not written yet. `warrior 3rd`, `rogue 3rd` and their 4th-tier files are still
    two-line placeholders (`BL-02`), so this lands the day you author them and not before.
  - 🔔 **THIS IS THE REMINDER YOU ASKED FOR.** When you open a warrior or rogue file, `precision` and
    `anti_magic` want rows in it. ⚠ **A class-skill-TABLE change needs a new APK** — the client builds
    its Learn tab locally — so it rides a client batch, not a server-only push.
  - ⚠ The level question the old entry asked (class change vs 76) is answered by this: a learn level
    is whatever the row says, so there is nothing left to rule separately.

- `BL-18` 🔵 **The nuker-vs-champion measurement (`0a`).** The nuker beats the champion by 19% in
  the matrix. You deferred the ruling to play: *"This need to be tested. When I leave the chars to
  play alone all measure."* ⚠ That makes auto-farm load-bearing for a balance decision — and
  auto-farm has never been through a long unattended run.

- `BL-19` ⏸ **Combat depth — held by you (2026-08-01).** Perfect/excellent block · position bonuses
  (hook reserved) · PvP and PvE damage multipliers (both hooks exist and are 1.0). *"the combat
  depth I don't want it build for now defer it."* Not dropped — do not build unasked.

- `BL-71` ✅ **BUILT 2026-08-13/14 (0.64.0)** — the whole aggro/taunt model. Taunt POWER is an authored
  per-level field, Provoke is a 5-rung ladder (1500 → 5100 across 20/24/28/32/36), threat decays 1%/s,
  **heals generate `power / castSeconds × 10 × people`** and **buffs `grantLevel × 20 × people`** (your
  2026-08-14 rulings — for buffs it is the LEARNED level, so one taken at 50 is worth less than one
  taken at 70; and both scale with how many the cast reached),
  and the proximity-pull defect is fixed (a pull seeds 5% of the mob's own max HP). See `CHANGELOG.md`.
  Delete at the next sweep.
  - ⚠ **Your buff:heal ratio holds at the TOP of the game and not at 35 — and you closed that on
    2026-08-27.** You sized it against "a quick heal with ~1500 power at that lvl". The **2nd-tier**
    ladder stops at power 301 (learned at 35), so a group buff out-threatens a heal there by ~8×
    instead of ~1.3×; the **4th-tier** ladder you have since authored reaches Ultimate Heal 1400-2000
    plus Healer's Power +2000, which lands the ratio where you wanted it. That was `BL-16`, and your
    ruling was that the 40+ rungs carry it rather than the 20-35 numbers moving. The 20-35 mismatch is
    therefore **intended**, not owed: a level-35 cleric is not meant to out-heal a group buff.
  - A full party is **9**, not the 7 in your example, so a level-70 group buff tops out at **12,600** —
    which is the intent (*"Full buffing a full party should take the agro from mobs for awhile"*).
  - The remaining 20-30k taunt rungs are levels 6-10 of the same ×1.36 ladder and belong to the
    3rd/4th class kits — blocked on `BL-02`, like every other 40+ number.
  - Not built and not asked for: a client-visible aggro list.

- `BL-84` 🔴 **RENAME EVERY SKILL ID TO MATCH ITS NAME — UNBLOCKED 2026-08-20: THE HEALER IS DONE.**
  ⏰ This is the reminder you asked for. The trigger you named has fired — `healer 3rd.csv` is built and
  shipped in 0.74.0 — so this is now next in the queue whenever you want it, not a filed idea.
  2026-08-17: *"After the healer is done I want to change all the game skills id's to match the skill
  names ... not `lb_elf_dawn` <> Healer's Blessing, it should be `healers_blessing` or something that
  matches it. Make a note to remind me after the healer is done (I want all the skills, not only the
  healers — all 1st, 2nd + healer 3rd)."*
  **Scope, his**: every skill in the **1st** and **2nd** class tables plus the **healer 3rd** — not the
  healer alone. The other seven disciplines follow when their CSVs land, so the convention has to be
  settled here and then simply obeyed.
  🔑 **Why it is worth doing**: the ids were named after the SLOT a skill sat in, not the skill. Three
  level-40 healer ids now openly contradict the thing they identify — `lb_elf_dawn` is *Healer Blessing*,
  `lb_human_mend` is *Quick Great Heal*, `lb_ork_font` is *Healing Totem* — because each was reused when
  his authored row landed on its slot. That is the right call for data (see `BL-02`) and the wrong one
  for reading code, and it gets worse with every CSV he writes.
  ✅ **NO MIGRATION NEEDED — he settled it the same day**: *"I'll reset the db anyways so it's not of a
  concern."* Ids are persisted (learned skills + the skill bar's `SkillBarCsv`), so a rename would
  normally orphan every character's bar — the failure `retired-skill-ids-leak` recorded once. A DB reset
  removes that entirely, which turns this from a migration into an ordinary rename. **Do it in one pass
  while the reset is happening**, not spread across versions, or the two halves meet in a live DB and the
  problem comes back. ⚠ Ids also appear in `docs/` and in the premium/consumable catalogs, and
  `SkillCsvSeed` matches CSV rows to code **by NAME**, so the checker can neither verify this pass nor
  catch a mistake in it — the compiler is the only safety net, which is fine for constants.
  🔵 Convention to settle with him before starting: strip the `lb_`/discipline prefixes entirely, or keep
  a short one for per-race variants that share a display name across races?

---

## Items & economy

- `BL-20` ✅ **BUILT 2026-08-14 (0.66.0)** — a partial pick now leaves the box in your bag carrying the
  picks you didn't spend (`InventoryItem.PicksRemaining`), and it is consumed only when the last one
  goes. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-21` 🟡 **Per-mob and per-zone drop identity.** *"I would like obe mob to drop let say a sword
  and a 2h sword, the other to drop only main armors, third boots and helmet … to go to a spot and
  know I can get there light armor and 2h-sword."* Then: *"later I'll want a 'ork settlment' where
  are 5 different demon types and I go there for lvl up, and several different settlements and zones
  with meanings."* You gated this yourself behind the world-map/positions pass (`BL-45`).

- `BL-22` ✅ **BUILT 2026-08-14 (0.66.0)** — a **Break down** button on any unworn tiered piece: rarity
  → the material's rarity, grade → the amount, and no gold, because *"u give up gold to get mats"*.
  See `CHANGELOG.md`. Delete at the next sweep.
  - 🔴 **YOUR BUDGET IS NOT REACHABLE AT S, AND NO TUNING CHANGES THAT — this needs your ruling.**
    Measured (new `BalanceMatrix` `M13`): D **−10%**, C **−18%** — inside your 10-20% — but B, A and
    **S all move 0%**, so a fully S-geared character stays at **347h**. Cause: *"rarity for mats
    rarity"* means salvage can only pay the rarity of the gear that DROPS, and gear rarity is capped by
    RANK, not band — a normal mob stops at Epic and **an elite stops at Epic too**; only a BOSS drops
    Legendary/Mythic gear, at 0.09 kills/h. The A and S recipes bind on **Legendary Ingot**. At a
    uniform quantity of 20 the early rungs collapse to −24/−39/−72% while A and S *still* move 0.00%.
    Your three options, none of them invented: **(1)** accept it as a mid-game feature (what ships);
    **(2)** let elites drop Legendary gear — opens a gear faucet that competes with crafting;
    **(3)** let a high grade bump the salvaged rarity a rung — contradicts "rarity for rarity".

- `BL-23` 🔵 **The coin curve — MEASURED 2026-08-27, and it is not the problem the old entry claimed.**
  You replaced the assertion with a measurement request: *"i want potion/rune per hour consumation and
  golddrop/h .. to compare for fewe lvl rangees - for now at lvl 43 i have 5kk + gold so it dont seem
  like a problem."* Built as **`dotnet run --project tools/BalanceMatrix -- --goldflow`**, off the real
  drop tables, the real vendor prices and the real damage formulas. What it says:
  - ✅ **YOUR 43 READING IS EXACTLY RIGHT.** At 43 a farming character nets **740k-1,010k gold/hour**,
    so your 5kk is five to seven hours of play. The model and your save agree without tuning either.
  - ✅ **POTIONS ARE NOT A COST.** Priced at the cheapest tier that can actually SUSTAIN the deficit,
    potion burn is **0-3% of income at every band from 20 to 76**, and 10% in the single worst case
    (the level-85 nuker). There is no potion economy to fix; regen covers most kits outright.
  - 🔴 **THE RUNE IS THE REAL COST, AND ONLY WHEN YOU ARE POOR.** A 1h War Rune box is 150,000 flat.
    At 20-30 an hour of farm buys **2.4-2.9 hours** of rune (~35-40% of income); by 61 it buys 25, by
    85 it buys 37. So the rune is a **newbie tax that evaporates** — the opposite shape to a drift.
  - 🔴 **THE DRIFT IS REAL BUT IT IS 5.4×, NOT 51×.** Hours of farm per chest piece of your own tier:
    **1.68 at 20 → 0.50 at 40 → 0.46 at 61 → 0.31 at 76.** Monotone downward apart from the expected
    bump inside a tier (price is flat from 40 to 51 while gold keeps climbing, which is why 43 reads
    better than 40).
  - 🔴🔑 **THE SHARP FINDING IS A CLIFF AT 80, WHICH THE OLD ENTRY NEVER MENTIONED.** S grade is
    **top-half only** — Epic/Legendary/Mythic, no Common rung exists (`ItemCatalog.IsTopHalfOnly`) —
    so the cheapest level-80 body is **126,000,000** and an hour of farm buys **0.04** of it: about
    **26 hours per piece**, against 3 hours at 76. That is not the coin curve drifting, it is the
    gear ladder stepping, and it is where a fix belongs if you want one.
  - ❓ **What I need from you.** Three separate calls and they are independent: (a) is 26h/piece the
    intended endgame grind, or does S want a cheaper rung; (b) does the 150k rune want a cheaper
    low-level box, since it is only ever felt before 40; (c) is a 5.4× drift across 20-76 acceptable
    as pacing? **Nothing moves until you say** — every rate here is one you have already tuned once.

- `BL-25` 🔵 **The drop-group simplification — half built, half unquotable.** *"In a way I want to
  simplify it"* — the inner roll should pick the drop **directly** rather than picking a rarity first,
  with per-item control (your example: a rarer Scroll of Resurrect inside its own group).
  ⚠ **Re-marked 🔵 on 2026-08-14.** The **per-item half SHIPPED** — `RateConfig.DropItemRates` plus
  `/droprate item <id> <mult>`, which is your Scroll-of-Resurrect example working today. The other
  half has **no surviving verbatim quote anywhere in the repo**, and the current shape is deliberate:
  the comment at `MobCatalog.cs:262-265` records that one group per (family, rarity) is what lets a
  BOSS row summing past 100% (E 70 + L 40 + M 2) drop several pieces at once. Collapsing the groups
  would break boss multi-drops and move a measured economy. **Say it again in your own words and it
  goes back to 🔴.**

- `BL-27` ✅ **BUILT 2026-08-14 (0.66.0)** — `set_robe_t61_sup` / `robe_t61_sup` ("Bloodsteel Raiment"),
  the tier's SUPPORT robe, straight off your CSV row. *"Stun/Fear Resist x1.7"* folds to `CcResist 0.4`
  — the same fold already shipped on the heavy and light `611` rows. See `CHANGELOG.md`. Delete at the
  next sweep.

- `BL-28` ✅ **BUILT 2026-08-27 (0.92.0, retuned 0.92.1) — MP POTIONS, PvE ONLY.** The hold came off the
  day `--mpdrain` measured the economy the 40+ kits had created (0.91.2). Three tiers mirroring the
  healing ladder's rates — **20 / 70 / 150 MP/s for 15s on a 30s reuse**, so **10 / 35 / 75 sustained**
  — at **double** the healing ladder's price: **120 / 500 / 3,000**. 🔴 **They do not drop anywhere**,
  which is what the double price buys: *"common/uncommon healing potions are dropped so u dont spend
  there … u need to buy mp pots"*. Two sources only — the Apothecary shelf for Common and Uncommon, the
  Potion Master's craft L5 for the Rare (*"its raiding support item that is economy player trade
  only"*). **PvE only, gated on the DRINK not the effect**: a potion already running ticks out its full
  15s when you flag, and only the next bottle is refused. 🔴 **Boss fights are deliberately allowed** —
  the gate is the PvP flag and a boss is PvE; that is a ruling, not an oversight.

- `BL-29` ✅ **BUILT 2026-08-26 (0.87.1) — SP BOTTLES.** An SP Broker takes 1e9 SP + 100kk gold and
  hands back a tradable bottle, with a confirm step before you drink one. 🔑 The point that outlived the
  feature: a skill can be priced in ITEMS (`LearnConsumableId`), which is what keeps `SkillPoints` an
  `int` honest rather than forcing a widening.

- `BL-30` ⏸ **Recipe drops below A grade** — no recipe item exists under A (below 76 they are
  learned by level). Add the same way A+ was added, when there is a reason to.

- `BL-76` 🔴 **BOSS SKILL GEMS — a boss drops, for its own level, a gem that grants a skill.** Your
  design, 2026-08-15: *"A bosses to drop for their lvl a special skill gem .. 3 rarities ..
  Epic/Legend/Mythic ... Chance for boss like 50% for a epic ... 5 for l and 0.5 for myth ... A epic
  can get u a magic or a physical dmg skill for the current lvl that do 1:5 of a nukers/fighters skill
  as dmg .. A legend can get u a passive that increase pvp/pve atk/def + 1:2 skills dmg ... And myth
  can also increase a stat +1 (at random) with 1:1 dmg and higher % for pvp /pve dmg."* Your closing
  clause is part of the spec: ***"the % and values can be then altered"*** — the numbers below are
  placeholders you have pre-authorised to move, so do not treat a retune of them as re-speccing you.

  | Rarity | Drop chance / boss | What the gem carries |
  |---|---|---|
  | Epic | **50%** | one damage skill (magic OR physical) at the boss's level, **1/5** of the class skill's damage |
  | Legendary | **5%** | a passive: PvP/PvE **atk + def** — plus the skill at **1/2** damage |
  | Mythic | **0.5%** | the Legendary passive at a **higher** PvP/PvE %, **+1 to a random stat**, skill at **1/1** |

  🔑 **Why this one is worth building even before the numbers settle:** it is the first content that
  makes a boss kill matter *for its own sake* rather than as a lump of EXP, and it is the only reward
  in the game whose value is not on the gear ladder. It also gives the **PvP/PvE damage multiplier
  hooks a first real consumer** — they exist and are hardcoded 1.0 today, reserved under `BL-19`, which
  you are holding. A Legendary gem is what turns them on, so this entry is where that hold gets lifted.

  🔵 **Five shape questions, all small, all answerable at build time — none of them blocks queueing
  this.** Recorded now so the build does not invent them silently:
  1. **Is a gem consumed into a permanent learn, or is it worn?** "Get u a skill" reads as consumed.
     But a stat +1 and a PvP passive read as *equipment* — and a worn gem needs a slot, which the
     paperdoll does not have. Consumed-and-learned needs no new slot and no new UI.
  2. **What decides WHICH damage skill?** Rolled at the drop (so a gem is a lottery you can trade) or
     picked by the holder (so it is a reward you steer). Trade value differs completely.
  3. **"For their lvl" — does the gem carry the BOSS's level or the opener's?** A level-20 boss gem
     used at 60 is either dead weight or a free rung, and those are opposite economies.
  4. **Duplicates.** A second Epic gem of the same skill — refused, upgraded, or a second copy to sell?
  5. **`1:5` of WHOSE skill?** A nuker's and a fighter's top skill at the same level are not the same
     number, so the ratio needs one named reference skill per channel or it drifts by class.

  ✅ **The boss curve underneath it is no longer unruled** — this used to say *"a flat ×100 swings boss
  difficulty 11× between level 20 and 76"*, and `BL-13` fixed exactly that in 0.89.0: every boss in the
  game (44 / 60 / 65 / 90) now takes an 18-23 minute party fight. So a 50% gem drop no longer makes the
  lowest boss the cheapest gem in the game by fight length. ⚠ What is still uneven is the **EXP** it
  pays (`BL-49`, which you ruled *"leave it"*), so a lower boss remains the better hour in exp terms —
  worth knowing when you set the gem %, which are explicitly yours to move.

- `BL-77` ✅ **BUILT 2026-08-16 (0.69.0)** — the PvP flag is the area filter, for every AOE and every
  no-damage skill at once, and it pairs with the reflect fix from the same pass: *the flag follows
  intent*. See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ **Three shape questions were open and I answered them as the shape every other system here
    already has** — party excluded from an area cast, support not routed through the rule, and only the
    ACTOR flagged (never the person revealed). Each is marked as mine in the source and on checklist
    row `87c`. **Re-rule any of them and it is a one-line change**; nothing depends on them.
  - ⏳ **The second warrior class is AOE and still does not exist** — that is `BL-02` authoring. It
    inherits this rule with no work: the filter lives in the shared area enumeration, not in a skill.

---

## Classes & skills

- `BL-95` ✅ **BUFF PRESETS — BUILT 0.99.0, and the NPC set grew to SIXTEEN.** Your 2026-08-28 list
  added Serenity, Soul, Aim and Agility to the twelve, *"players to not be so overwhelmed by mobs
  (serenity, soul — longer mage sessions; agility+aim — fighter less misses, dagger less hits
  taken)"*, and named the two role presets by hand.
  - **Four buttons at the NPC now**: **Full** (16), **Mage** (10 — Bulwark, Force, Alacrity, Swift,
    Ward, Body, Soul, Serenity, Resolve, Frenzy), **Fighter** (10 — Bulwark, Might, Fury, Swift, Ward,
    Body, Vigor, Vamp, Frenzy, Aim), and **Custom** once you save one. Each row shows the COUNT as
    well as the price, because the buff bar caps at 20 and the full set is now 16.
  - **Save reads what you are WEARING**, your workflow exactly — buff fully, cancel what your class
    doesn't want, press Save. Both worked examples fell out of one filter with no special case: a
    buff records the def that *created* it, so a potion resolves to its family rung (never
    `npc_might`) and a group like Feral Bloodlust is ONE buff under the group's own id — so neither
    can leak into a preset.
  - **[Save] alone until you have one; then [Custom] [Save] [Delete]**, with Save asking before it
    overwrites and Delete asking before it deletes. Saving with no blessings on you is refused rather
    than stored, so a [Custom] button that casts nothing can never appear.
  - **PER SUBCLASS** — the question you flagged, answered the way the auto-marks bug says to answer
    it. `Subclass.BuffPreset`, new `SubclassRecord.BuffPresetJson`. ⚠ **game.db delete required.**
  - Also fixed on the way past: the NPC's accuracy single was displayed as **"Accuracy"** while the
    ladder, all three potions and all three scrolls call the family **"Aim"** — one blessing wearing
    the stat's name. It is `Aim` now; the id is unchanged.
  - 🔵 **The one number worth watching in play**: 16 against the cap of 20 leaves four free slots, not
    the eight the trim to twelve bought. The presets are the answer (ten leaves ten), but if the full
    set plus a party buffer feels tight, the fix is to take Mage/Fighter rather than to trim the set
    again.

- `BL-96` ✅ **THE `AOE` COLUMN — BUILT 0.94.2 on your go-ahead.** `LEARN, NAME, TYPE, RANGE, AOE,
  TARGET, …` across all 24 files, 1,425 rows, by a new `SkillCsvSeed --aoe-column`. `--check` now
  verifies the radius against `SkillDef.AreaRadiusAt`, so it is a **CHECKED number for the first
  time** instead of prose in the DESCR cell. Elemental Wave reads **`0,200,enemy/aoe`** — your worked
  example — and Arcane Wave **`900,400,enemy/aoe`**. It also settles the contradiction between your
  2026-08-27 ruling (the TARGET column does NOT encode where the circle sits) and your 2026-08-28
  description of Elemental Wave as `self/aoe`: with two columns, neither has to carry the other's
  meaning. `README.md` in the CSV folder documents the three columns.
  ✅ **AND YOU ANSWERED THE ONE OPEN QUESTION** (2026-08-28): the party heals read `600,600` because
  the range gate applied to the targeted ally, and you ruled the GATE should go — *"should be cast
  able without a target .. So 0/x"*. Range is 0 on all of them now (0.94.3), which also means they
  fire with an ENEMY selected. Nothing left open here.

- `BL-100` ✅ **BUILT 2026-08-28 (0.97.0) — EVERY CLASS RENAMED TO YOUR TABLE.** *"now just sound
  over complicated ... but I want it simpler ... All races are the same until lvl 40 so we can call it
  elf-A human-A"*. The 2nd class is race+role now, and the six best words we had (`Assassin`,
  `Sentinel`, `Templar`, `Shadowblade`, `Stalker`, `Champion`) moved onto 3rd classes that earn them.
  ➡️ **[docs/design/ClassRenames.md](design/ClassRenames.md)** is the live roster and the record.
  🔑 Cost was zero: nothing persists a name, so no save broke and no `game.db` reset was needed.
  ⚠ Four built lines differ from your written list, each explained in that file — the biggest being
  **elf `Swiftblade → Sword Saint`**, because your `Sword Master` at the elf 3rd collides with the
  human 4th and `DuplicateNames()` has had no exemptions since `BL-97`: a hard startup failure, not
  just the smell you spotted.
  ✅ **AND THE LAST FLAG IS CLOSED TOO** (2026-08-28, 0.98.1): `Sword Dancer` — the one 3rd chosen to
  rhyme with its 4th rather than be its lesser form — is **`Skirmisher → War Storm`**, which puts the
  war_aoe 3rd tier in one voice: Vanguard / Skirmisher / Warborn. 🔑 Your *"anything aoe is War
  named"* rule holds across all six AoE/support 4th classes and is written into `ClassNames`.
  ✅ **ALL THREE OPEN ITEMS CLOSED THE SAME DAY, by you:**
  1. **`Sentinel` clashed with the MODERATOR's worn title.** You kept the class and rewrote the whole
     ladder: *"supreme being(owner) -> god(admin) -> demi god(mod) -> warden(chat mod) -> player"*.
     Two plates changed (Sentinel → **Demi God**, Silencer → **Warden**) and the four now read as one
     descending order instead of four unrelated words.
  2. **`Light Bringer` → `Holy Priest`**, which also ends the confusion with the demon healer, and
     gives the human line a ladder you can hear: Human Priest → Holy Priest → Holy Messenger.
  3. **The bow order** is `Soultracker → Soulhunter` on the demon, your own demon row's order.

- `BL-101` ✅ **BUILT 2026-08-28 (0.98.0) — THE THIRD RACE IS `DEMON`.** Your idea, and it earned
  itself twice over: **`Orc Archer` is already a level-12 MOB**, so the player race was sharing its
  name with common trash — and it killed the last naming exception, because the support line only had
  to hide behind `Shaman` while "Ork Priest" sounded like nothing. All fifteen 2nd classes are
  race+role now with no special case.
  🔑 **`Race.Demon` is still value 2** — a character persists the number, so every save is the same
  race under a new name. No `game.db` reset.
  Swept: the enum and all 17 code files (compiler-verified), 162 RACE-column cells across five CSVs,
  and the prose in the live docs. ⚠ **Owner quotes were left VERBATIM** — 35 lines carrying a `*"…"*`
  quote still say "ork", deliberately, and so do CHANGELOG / Roadmap / Playtest-Archive, which are
  historical records.
  ⚠ Of the three IG names in your demon column: **`Warlock` stays** (yours is a buffer, theirs a
  summoner — different role), **`Hell Knight` → `Dread Knight`** by your own swap, and `Dreadnought`
  was only ever an alternative. `Juggernaut` went back to being unused — *"sounds orkish"*.

- `BL-97` ✅ **BUILT 2026-08-28 (0.96.0) — THE TEMPEST AND THE VANGUARD ARE RETIRED. Eight choosable
  paths per race, 24 third classes.** You ruled: *"Tempests must go .. And elf nuker 3rd is starweaver, ork is
  cinderwitch and human stays magus"*, then *"Remove the vacant tank as well — the 3 tanks must have
  their name and the other is the same for the 3 races ... So is the one that must go"*.
  🔑 **Your test for which of a pair dies is the keeper**: both retired disciplines wore ONE name
  across all three races (Vanguard/Doomward, Tempest/Skybreaker), because neither was ever really
  three classes. The Vanguard also taught nothing — the 2026-08-10 purge took its learn lines — so a
  level-40 Knight could pick an empty class.
  ⚠ A retired discipline's NAME is free to reuse on a live one (a name is not an id) — which is what
  your `champion -> sword master` / `vanguard -> war master` rename does with it.
  🔑 **The three names were ALREADY exactly that** — `ClassNames` has read Magus / Starweaver /
  Cinderwitch since the per-race naming pass of 2026-08-17 — so the ruling's naming half cost nothing
  and the whole pass was the retirement.
  🔑 **And it deleted NO authored row.** My earlier note here warned it would; that was wrong.
  `nuker 3rd.csv` carries no discipline column, so the 208-row kit was registered to Magus **and**
  Tempest, identically — the retirement removed a duplicate registration, not content. `--check` is
  still clean.
  What actually changed: `Disciplines.Of` returns a NULLABLE second branch and the nuker's is null;
  third-class ids **112 / 124 / 136** and their ascensions **212 / 224 / 236** are now permanently
  vacant — as are the tank's **102 / 114 / 126** and **202 / 214 / 226** (the id is computed from the
  parent, so nothing else moved); the twelve class-proof quest items for those ids are gone with them
  (1080 → 1068 items); a character saved on a retired discipline is migrated to its surviving sibling
  on load rather than being left classless; and `ClassNames.DuplicateNames()` has no exemptions left.
  ⚠ One consequence worth knowing: **"two nukers" is no longer a legal subclass pair.** The one-per-
  discipline rule stands, so a second Sorcerer/Inquisitor/Witch would have to walk the Magus twice.
  Delete at the next sweep.

- `BL-87` ✅ **BUILT 2026-08-23 — THE BUFF CAP IS 20, AND WHAT COUNTS IS A PER-BUFF FLAG.** Playtest 27:
  *"we need make max buffs limit. Now I have 24 buffs as healer ... So if we make it 20 then the buffer
  becomes a must"*, then his rules: *"A self buff that is 20min still counts as a buff while a self 30s
  buff is temporary and is not .... For example the bow expertise is a buff that counts toward the limit
  ... the flag is not self or not, the flag is per buff .. default is true (counts towards max) - toggle
  don't and heals etc"* · *"FIFO ..1st buff buffed gets removed"* · *"If a buff is not counted u can have
  20+14"*. Delete at the next sweep.
  - 🔑 **Half of it was already built and he had been living inside it.** A `MaxBuffSlots` cap with FIFO
    eviction has existed since the buff-ladder work — at **24**. That is why he counted exactly 24: he
    was *at the cap*, and buffs had been quietly falling off the back of his bar. `24 → 20`.
  - **New `SkillDef.CountsTowardBuffLimit`, default TRUE**, authored `false` on the temporary ones. NOT
    derived from `TargetMode` (Bow Expertise is `SelfOnly` and counts) and NOT derived from duration
    (that is a consequence, not the rule). ⚠ Read off the buff that **LANDS**: a one-child wrapper
    resolves to its child, so Dash is flagged on `buff_dash_*` and a Might potion is not flagged at all —
    it IS a single of the might family, out of a bottle, and pays its slot.
  - **Authored `false`** (every one ≤90s, which is what made the line easy to draw): the six Combo Rush
    rungs · War Cry / Greater War Cry · Battle Fury · Fortify · Shrouding Hymn · the three racial Renew
    verses · Harmony of Restoration (the party HoT) · Aegis · Battle Presence / Battle Defence · Conceal ·
    Defensive Wall · Evasion Boost · Indomitable · Last Stand · Mana Barrier · Meditation · the eight
    Dash/Sprint rungs · the three healing potions. Toggles, debuffs and the gear/rune row were already
    excluded by the engine and still are.
  - **The measured result: a fully-buffed character sits at 16 / 20**, four free. Self-serving off the
    NPC buffer cost **19 of 20** for a strictly weaker set — a group packs three or four families into
    one slot and a single never can. **The cap limits the alternative to the buffer, not the buffer.**
    ⚠ **That 19 is now 11** (0.81.0): playtest 28 cut the NPC set to his eleven, so the NPC bar and a
    real buffer's groups can finally coexist. The conclusion is unchanged and better served — what the
    cap squeezes is still the substitute, it just no longer squeezes it to the point of absurdity.
  - **Verify it with `dotnet run --project tools/BalanceMatrix -- --buffs`**, which prints a `SLOT` /
    `-` column using the same rule the engine uses, resolving wrappers the way `ApplyBuff` does. It was
    the tool that caught a bulk edit wrongly exempting the three **Swift** rungs.
  - ⚠ `GameLoopService.BuffPlan` was made `public` so the census reads the real resolver rather than a
    copy of it. ⚠ No CSV column changed — `--check` is green on all ten files.


- `BL-89` 🟢 **BUILT 2026-08-26 (0.86.0) — THE CHAT LOG HAS A READER.** Playtest 28, your question:
  *"don't we need a chat log … because now an admin/mod should ban based on som1 is trying to sell u for $
  on private chat"*. The WRITE half shipped in 0.81.0 and nothing ever opened it; `/chatlog` is the way a
  moderator on a phone reads it, which was always the point.
  - ✅ **All three shapes you asked for, and they combine:** `/chatlog <name>`, `/chatlog <name> -w`
    (whispers only), `/chatlog around <time>`, plus `-p <page>` on any of them. Into the System tab,
    25 lines a page, oldest-first, staff-only.
  - ✅ `around` takes **`15m` / `2h` / `1d`** first, because that is what a report actually sounds like —
    *"about ten minutes ago"*. `11:02` = today UTC; `yyyy-MM-dd HH:mm` names the day. Everything is UTC,
    since that is what the rows hold.
  - 🔑 **It flushes before it reads.** Lines wait for the 60-second autosave, so a straight query would
    have been blind to the last minute — and the case this exists for is a LIVE report. Typing `/chatlog`
    the moment you are told and seeing nothing would read as innocence.
  - ✅ **RETENTION RULED 2026-08-26: 90 DAYS.** *"90 days retention no point in keeping more .. if some1
    gets reported .. must take no more than a week to deem him banable or not"*. Live in 0.86.1 —
    `ChatLogRetentionDays` = 90 and the six-hourly sweep is deleting. 🔑 The window is sized to how long
    a CASE stays open (a week), not to how long the evidence stays interesting; **don't raise it "to be
    safe"** — that is the instinct the ruling rejected.
  - 🔴 **THE PRIVACY QUESTION, ANSWERED PROVISIONALLY — reverse it in one line if you disagree.**
    **Moderator and above read whispers** (they hold the jail and the kick, and the private channel is the
    case the feature is for). **A Chat Moderator reads PUBLIC channels only** — that rank goes to someone
    you do *not* fully trust (playtest 26), and a mute needs no private mail to justify it.
- `BL-90` 🟢 **BUILT 2026-08-24 (0.83.0) — A PER-SKILL DEBUFF SUCCESS MULTIPLIER, AND THE ROUTING BUG IT
  EXPOSED.** Your final shape: *"DebuffLandMod should be floating one value - default 1 … armor/weapon
  break + gravity + Arcane/Fros/Pyro blasts(nuker 3rd) should be 75% at parity (x1.5) and the other should
  be 25% at parity (x0.5)"*, with the values themselves authored into the CSVs as `(success chance xN)`.
  - ✅ `SkillDef.DebuffLandMod` + `SkillLevel.DebuffLandMod` (0 = inherit), read as `DebuffLandModAt(lvl)`
    — one plain float, default 1, per skill AND per rung. **No tier constants**: a first pass had four and
    you replaced them with the CSV column, which is the right home.
  - ✅ 🔑 **THE ROUTING FIX YOUR ARITHMETIC FORCED.** ×1.5 only reaches 75% off a **50%** base, and 50% is
    the CONTESTED curve — the fizzle path is ~99%. Armor Break, Weapon Break, Gravity and Mana Strain all
    declared `DebuffSchool.Magical`, all said *"Contested ATK vs SPT"* on their cards, and all took the
    **fizzle** roll anyway, because the branch tested the effect-FLAG mask and never read the school.
    `IsContestedDebuff` now reads both. Those four moved from ~99% to 75/50/75/25% at parity.
  - ✅ `SkillCsvSeed --check` **reads the `(success chance xN)` column** (119 authored rows). It needed a
    carve-out: the DESCR reader strips parentheticals as commentary, which was eating every one of them.
  - ✅ `BalanceMatrix` prints **=== DEBUFF SUCCESS ===** — your scale, every tagged skill, and the four
    that the routing fix moved.
  - **Applied from your CSVs:** Armor Break ×1.5 · Weapon Break ×1.5 · Gravity ×1 · Bind ×0.7 · Mana
    Strain ×0.5. **From your general rule, MAGICAL only:** Entangling Roots, Warding Step ×0.5.
  - 🔑 **PHYSICAL DEBUFFS STAY ×1** — your ruling: *"physical debuffs should be x1 for now .. Con saves ..
    we deside later"*. The physical school already contests **CON**, a stat a fighter really carries, so
    taxing it too would double-charge. Reverted on Shield Stun, Shield Bash, Stay!, Terrifying Roar.
    ⏳ Explicitly a HOLD, not an answer — re-raise when physical CC is actually measured.
  - ⚠ **YOUR MESSAGE AND YOUR CSV DISAGREE ON GRAVITY** — the message puts it in the ×1.5 group, all
    fourteen rows in `healer 3rd.csv` say `(success chance x1)`. The CSV won, per the standing rule. Say
    the word and it is one edit in both places.
  - ✅ **CLOSED 2026-08-27 — THE BURSTS AND THE WHOLE NUKER LIST SHIPPED IN 0.87.0.** You asked *"what
    about the 3 bursts ? they should be build as the chance for debuf per skill"* — they were, three days
    earlier, and **this entry was simply stale**. Verified in `Skills.Nuker3rd.cs`: Arcane / Frost / Pyro
    Burst all carry `DebuffLandMod: 1.5f`, and beside them Frost Spikes ×0.7, Frost Pierce ×0.5, Witches
    Curse ×0.7, Witches Scarecrow ×0.5, Arcane Void ×0.3 — every value your CSV authored. `nuker 3rd.csv`
    took its `Check.Specs` line the same day and `--check` is green on it.
    ⚠ 🔑 **THE LESSON IS ABOUT THIS FILE, NOT THE FEATURE.** Two entries (`BL-90` and `BL-91`) sat here
    claiming "not built" for three days after the code landed, because the kit that unblocked them was
    built under a different heading. **When a build closes a dependency, sweep the entries that named
    it** — the pass-end checklist rule already says to check the Backlog for stale marks, and this is
    exactly the failure it is meant to catch.
  - ⏸ **HELD BY YOU — Snare Trap and the Warchanter's stun-rider stay at ×1 until they are TESTED.**
    2026-08-27: *"the trap and stun leave them as still open until tested."* Both are hybrids that
    already exist on shipped classes; retro-taxing them is one line each and it waits for play, not for
    a decision. Not dropped.
  - ✅ **CLOSED 2026-08-27 — "buff removeal" needed nothing.** *"buff removal can be deleted .. arcane
    void is the one we need and later a demon tank probably will have a cancel too but thats still
    unauthored."* Arcane Void ×0.3 is built and is the cancel. An ork tank's cancel arrives with his
    authoring like any other unwritten row; it is not owed here.
  - ℹ️ Your *"(did the same for buffer)"* was a slip — *"did the same for healer ... my bad"*. Confirmed:
    `healer 3rd` and `nuker 3rd` carry the column, `buffer 3rd` has none and owes none.

- `BL-91` 🟢 **INTERRUPT IS IG'S OWN FORMULA (0.84.0) — BUILT AND FULLY RULED.**
  - ✅ **BUILT, IG's shape, with your two departures.** `FinalChance = (DmgTaken/MaxHP) × rand(1.00-1.20)
    × SPT-mod × (1 − resist buffs) × skill.InterruptMult`, + `InterruptPower` in percentage points.
    Your worked example reproduces exactly: 1000 on a 2000 pool, ×1, Resolve 54% → **23%**.
  - ✅ **Resolve is a PERCENT.** The ladder numbers did not move (18/25/36/40/42/48/54) and the CSV rows
    still read them; they are percentages of the incoming roll now. This answers the old QUESTION 2 —
    a flat buff on a growing pool always decays, a percent never does. ×0.46 at 20 and at 80 alike.
  - ✅ **The MEN curve, flattened to your numbers.** IG's 20 = ×1.00 / 50 = ×0.23 is ~4.8% per point and
    prices our level-39 human mage at ×0.395 — your *"a bit low"*. Ours is your alternative,
    **20 = ×1.00, 50 = ×0.67**, same geometric shape at a third the slope. Ours vs IG's on our bases:
    human ftr ×0.94/×0.78, elf mage ×0.85/×0.56, human mage ×0.78/×0.39, demon mage ×0.72/×0.29.
  - ✅ **No robe-set 50% resist** — *"and i dont want that"*. `StatCaps.InterruptResistMax` = 0.80 so any
    future source stacks into a clamp instead of multiplying past it.
  - ✅ **THUNDERSTORM IS FIXED BY THE MODEL, not by a patch.** 0.83.0 priced a cast against its own DPS, so
    a 300s reuse made the biggest nuke in the game the easiest cast to break. Reuse is not an input any
    more. A long cast is simply a cast that eats more hits.
  - ✅ Old QUESTION 1 is answered too: the *"58-94% per cast"* number came from DPS parity, which no longer
    exists. Measured now, a same-level fighter breaking a human mage's cast: **27% / 17% / 11% / 11%** per
    basic hit at 20/40/60/80, **12% / 8% / 5% / 5%** under Resolve.
  - ✅ **RULED: THE TWO NUKER INTERRUPT SKILLS ARE ×2** — *"add the nuker the two high interrupt skills a
    x2 chance. They are fast cast and x2 interrupt chance is good enough"* (2026-08-26). They are exactly
    the two rows in `nuker 3rd.csv` that say *"Higher chance to interrupt enemy casts"*: **Frost Spikes**
    and **Frost Pierce**, both `m.Atk +64` on a **2.5s cast / 1s reuse** at the top rung. All 28 rows now
    also carry **`(interrupt chance x2)`**, and `Descr.cs` reads that token, so it will be verified the
    day the kit is built. At 74, elf nuker vs a same-level human mage:

    ⚠ **RE-MEASURED 2026-08-27 — every number below moved, and not because of this skill.** `BL-78`.3
    rebuilt the PLAYER HP curve in 0.91.0, so the mage being cast at now has **3,239 HP** where this
    table's victim had ~1,280. A nuke is therefore a much smaller slice of him and every interrupt
    chance fell with it. The table is what `--goldflow`'s sibling `BalanceMatrix` prints today:

    | spell | dmg | % of HP | ×1 | **×2 (built)** | ×5 | ×10 | ×2 +Resolve |
    |---|---|---|---|---|---|---|---|
    | Frost Spikes | 160 | 4.9% | 4.3% | **8.5%** | 21.4% | 42.7% | 3.9% |
    | Frost Pierce | 160 | 4.9% | 4.3% | **8.5%** | 21.4% | 42.7% | 3.9% |
    | Elemental Blast | 270 | 8.3% | 7.2% | — | 36.1% | 72.1% | 3.3% |
    | Thunderstorm | 541 | 16.7% | 14.4% | — | 72.2% | 100% | 6.6% |

    Your ×10 guess came from expecting these to be small hits. Against the OLD pool they were not, which
    is why ×2 was the right call then. **Against the new pool they genuinely are small** — ×10 on a Frost
    skill is 42.7%, not the guaranteed cancel it used to be. At ×2 they read 8.5% per hit (3.9% through
    Resolve) and fire every ~2.5s, so a 4s cast now eats roughly 13% rather than a third.
    ❓ **Worth re-ruling when you next play a nuker**: ×2 was sized against a mage with a quarter of the
    HP he has today. Not changed unasked — the ruling stands until you move it.
  - ✅ **×2 IS IN THE CODE — YOUR WORDING FIX, 2026-08-27.** *"the x2 - nuker 3rd is build or atelast
    should be so fix the wording."* You were right and this bullet was wrong: the kit shipped in 0.87.0,
    **`InterruptMult: 2f` is on both Frost Spikes and Frost Pierce** in `Skills.Nuker3rd.cs`, and
    `nuker 3rd.csv` has had its `Check.Specs` line since the day the kit landed.
  - ✅ **AND THE ONE THING THAT REALLY WAS OWED IS NOW DONE.** `BalanceMatrix`'s interrupt table carried
    a hand-copied four-row literal (his CSV's top rungs, typed in while the kit was unbuilt). It now
    **reads the `SkillDef`s** — power, cast, reuse and the multiplier — so a retuned rung moves the table
    with it. 🔑 That literal is precisely why this entry was able to go stale while reading as current:
    a measurement that repeats an authored number instead of reading it will agree with itself forever.
  - **`BL-91` is CLOSED.** Nothing in it is outstanding; delete at the next sweep.
  - ⚠ `SkillDef.InterruptDefense` survives as a float FRACTION — the lever for *"this particular spell is
    hard to break"* without touching the caster's sheet. Unauthored.

    divide-by-zero. Unmeasured. Everything else in the model is your specification.

- `BL-92` ✅ **BUILT 2026-08-26 — BOTH HALVES. MP in 0.88.0/0.88.1, HP in 0.88.2.** You opened it,
  it was measured (`BalanceMatrix --mpregen`, then `--hpregen`), and you ruled every question the same
  day. A buffed level-74 mage was regenerating **288% of his own spell-spam cost**; he now sits at ~88%.
  The mastery `mpReg` ladder is FLAT and outside, SPT has its own curve, and **standing still is a
  stance**. Calm Spirit shipped with it. See 0.88.0-0.88.2 in `CHANGELOG.md`. Delete at the next sweep.
  - ✅ **THE HP HALF, ruled after you supplied IG's own numbers** (base 1.5-3.0 by race+class, ConMod
    CON 30→1.00 / 43→1.32, LvlMod `L/100 + 0.89`): *"I want to make the passives + not x as the mp ..
    and buffs to carry the multiplier .. and the flat is to added at the end"*. Every `hpReg` rung is
    now a flat +1.1…+2.7 HP/s and the flats sit outside. It ended the inversion that measurement found
    — a level-74 nuker on **27.5 HP/s against a tank's 16.4**, the class IG gives the lowest base regen
    holding the game's highest. Now warrior 18.0 > rogue 17.6 > tank 16.4 > nuker 12.9.
  - 🔵 **THE LEVEL TERM IS DEFERRED TO A PLAYTEST, NOT SETTLED.** *"Leave out lvl mod just leave the
    flat outside … So we will have x2 more than IG but not as much as we have now … Playtest will
    decide if it stays"*. We measure at **1.6-2.0× IG** at every level and the whole gap is ours
    (`1 + L/30`, ×3.71 across 1-85) against IG's (`L/100 + 0.89`, ×1.93) — which is character-for-
    character the `(level+89)/100` the damage formula already uses. `--hpregen` prints the swapped
    column ("if IG lvlMod") so the playtest has the number ready. **Do not take it without a ruling.**
  - 🔴 **THE FIGHTER FLATS ARE BACKWARDS AND YOU FLAGGED IT:** *"now fighters are not yet authored and
    have higher regen flat bonuses than mage"* — when the fighter 3rd/4th CSVs land, a fighter's `hpReg`
    flat must exceed a mage's. Today the nuker carries **+2.7**, the warrior **+1.6** (frozen at level
    32), the rogue **+1.2**, and the **tank none at all**; archer and dual have no `hpReg` row either.
  - ✅ **THE ORK BUFFER — CLOSED 2026-08-27, and it was already built.** *"the ork buffer have the hp
    boost skill so if its not build build it."* It does: `ClassSkillTables.Third.RegisterHpBoost`
    registers **HP Boost L1-L7 at 40/44/48/52/56/62/70 for the Warchanter of all three races**, Demon
    included, on your own 3rd-class SP ladder (36k → 390k, overridden per rung because the SkillDef's
    prices are the warrior's). So the demon buffer's extra HP comes from the KIT, which is the rule —
    identity is the skill kit, not a stat bonus — and no `hpReg` number was invented.
    ⚠ Note the distinction that made this look open: your original *"buffer ork should have more"* was
    about the **hpReg FLAT**, a regen number; HP Boost is a **max-HP** skill. It answers the intent, not
    the same field. If you did mean a bigger demon regen flat specifically, that is still unauthored.
  - ⚠ **The ladder stopped being progression**, knowingly: a nuker's six rungs from +1.1 to +2.7 used to
    buy +19 HP/s and now buy **+1.6 across 34 levels**. Same trade the `mpReg` ladder took. If those
    rungs should be felt, the FLAT numbers get re-authored bigger in the CSVs — not an engine change.
  - ✅ **EVERY PRIMARY STAT IS READ EFFECTIVE** (same day, second ruling): *"Need effective con to count
    on hp max/regen and whatever con have mod on … con armor set now will buy u nothing and atk-con
    won't hinder you"*. CON and ATK were the last two read BASE — by HP regen and by the character
    sheet / target panel, which sent `EffectiveWit/Agi/Spt` beside them. An armour set's `Con: -2,
    Str: +3` moved pool, regen and damage with **nothing on screen**. Fixed at all five sites; mob
    paths keep raw CON.
  - 🔴 **TWO ARMOUR ROWS FLAGGED, NOT CHANGED.** Your ruling was *"except armor masteries the 20%
    increase"*, so armour masteries kept their percents — but `rogue 2nd.csv` @36 carries `mpReg x1.8`
    and `tank 2nd.csv` @36 carries `mpReg x3.4`, which are weapon-mastery-sized numbers on armour rows.
    A tank at 36 therefore regenerates ×3.4. One-line change either way, waiting on you.
    ⚠ The HP pass makes this visible inside ONE row: `rogue 2nd.csv` @36 now reads `hpReg +1.2` (a flat,
    with every other hpReg) beside `mpReg x1.8` (still a percent, by the armour carve-out). Deliberate,
    and it stays odd-looking until you rule on the two mpReg numbers above.
  - 🔴 **`BalanceMatrix.BuildPlayer` NEVER SETS A 3rd CLASS**, found while building the report: every
    other mage table in the tool measures the level-35 kit at any level (no 40+ spells, no 40+ mastery
    ladder). `--mpregen` uses its own `BuildNuker`; the rest were deliberately left alone rather than
    silently moving every number the tool prints. Its own pass.
  - ⚠ Meditation's flat +25-40/s no longer gains from sitting — it was inside the stance multiplier on
    purpose ("sitting to meditate should pay") and the global flat rule outranks it. Knowingly traded.

- `BL-34` ✅ **BUILT 2026-08-14 (0.66.0)** — **Madness**, a party-cast Frenzy handing out a new **rung 7**
  of the family, at **76 on the Warchanter** so an admin can party-buff with it. Your deliberate
  temporary home — *"when the kits land we will move it"*. See `CHANGELOG.md`. Delete at the next sweep.
  - ⏳ **The 76+ buff EXPANSION half of this entry is still owed and was NOT re-ruled**: *"2-3 more
    Harmonies and 1-2 more improved buffs"*, plus the healer getting all the singles including a single
    Frenzy. Blocked behind `BL-02` like every other 40+ authoring.

- `BL-35` ✅ **BUILT 2026-08-14 (0.66.0)** — two level-83 skills, both carrying keeps-buffs-on-death **and**
  the auto-resurrect nothing used until now: **Rite of Preservation** (Lightbringer, on an ally, 100%
  exp back) and **Undying Will** (Bulwark, self). Ranks 2 and 3 on the existing `buff_preservation`
  key, exactly as the 2026-07-17 comment reserved them. ⚠ Your *"(not fixed)"* stands on the 1h/1h
  numbers. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-36` ✅ **BUILT 2026-08-14 (0.66.0)** — instant in a town or peace zone, a 5-minute wait outside,
  out of combat either way, and **entering a city neither cancels nor shortcuts a running timer**. See
  `CHANGELOG.md`. Delete at the next sweep.

- `BL-69` ✅ **BUILT 2026-08-13 (0.64.0)** — all three kinds. Hide is now withheld from the world
  snapshot itself (so nobody renders or can click it), broken by anything but movement, revealed at
  skill EXECUTION not at the click, and countered by `Signal Flare` (rogue/bow 28: reveal in 300 +
  30s no-hide). Stealth is a buff-carried, action-proof, unaggroed-mobs-only effect delivered two
  ways — `Prowl` (rogue toggle at 20, 1 MP/s) and `Shrouding Hymn` (cleric party version at 30:
  1 min / 30s / 300 MP). `/invis` is absolute and manual-only. See `CHANGELOG.md`. Delete at the
  next sweep.
  - ✅ **Closed 2026-08-14: hidden is hidden from EVERYONE**, party and staff included. You are still
    in the party and still listed (status `Hidden`), you are simply not renderable, clickable or
    heal-targetable — skipped by party heals/buffs, both auto-target pickers and the manual ally cast.
    Death clears a hide, so a corpse stays resurrectable. Staff keep `/tp`, `/tpme`, `/jail`, `/where`
    (they resolve by NAME, never by sight). No protocol change.

- `BL-38` 🔵 **Pets and summons** — immovable totems, class pets, the mage summoner. Designed, never
  scheduled, never re-raised by you.

---

- `BL-75` 🔵 **The heal-at-0 skill wants a warrior/demon home.** Playtest 23, on the old Undying Will
  behaviour: *"That idea for undying skill is good for a warrior ork, when he must die just heal himself
  30%"* — and *"as I said good skill for a warrior"*. 🔑 **It is already built and needs no new mechanic**:
  `LastStand` (`SkillEffect.LethalSave`, revives to 50% of max HP off a fatal blow, buff consumed) has
  been in the catalog the whole time; its learn line went in the 40+ purge. What is missing is only a
  **class + a level + the percentage** — which is 40+ authoring, so it waits on `BL-02` with everything
  else. Your two words to settle when you get there: is it Ravager/Warlord or race-gated to the demon, and
  is the number your 30% or the skill's existing 50%?

## UI & client

- `BL-41` 🔵 **A grade filter on the craft Gear page.** 62-63 rows is a long scroll on the phone.
  The question was put to you and never answered.

- `BL-42` ✅ **BUILT 2026-08-14 (0.66.0)** — `SkillText.Mechanics` states every FIELD-carried payload with
  its numbers, per level, on both the skill card and the Learn preview; the conditional lines now carry
  their condition ("Block chance (with a shield)"). 🔑 The cause was structural: the `SkillEffect` enum
  has been full for years, so every mechanic since has been a plain field, and the card read only flags
  and magnitudes. See `CHANGELOG.md`. Delete at the next sweep.


- `BL-44` 🟡 **"Everything is a skill" — the last two pieces.** Armor sets and weapon specials are
  still `StatMods`, not skills, so **buff-bar row 3 (item effects) is permanently empty**; and the
  set tooltip's **shield row** has nothing to show until shields belong to sets. You called this
  optional at the time.

- `BL-74` 🔵 **The phone still does not treat the app as a game** — playtest 23: *"as of 0.67.2 still
  game launcher don't treat it as a game. May be because of its development installation not store one.
  Dunno. Need to research how the phone and when it treats an app as a game."* Everything a manifest can
  claim is already claimed and shipped in 0.67.0 (`BL-46`): the duplicate LAUNCHER activity is deleted,
  `android:appCategory="game"` and Samsung's older `isGame="true"` are both declared, and exactly one
  launcher entry stands behind them. So the remaining variable is **outside the manifest** — One UI's
  Game Launcher is known to classify partly by Play Store category and install source, which a sideloaded
  debug APK has neither of. 🔵 **Owed as RESEARCH, not a build**, and it cannot be verified from here:
  it needs your device (does Game Booster's "add app manually" find it? does a release-signed APK behave
  differently from a debug one?). Nothing is broken in the game either way.

- `BL-45` 🔵 **The presentation pass.** Your words, still true: *"no sounds, a bit woody, no good
  visuals."* The loudest remaining gap. **You have reserved it for its own discussion** (2026-08-14:
  *"45 is a separate discussion later on"*) — do not start it piecemeal.
  ⤷ 🆕 **The VISUAL half of it now has its own id and its own conversation: `BL-93`.** `BL-45` keeps
  the rest — sound, feel, feedback, polish.

- `BL-93` 🔵 **IN-GAME VISUALS — MODELS, TERRAIN, THE LOOK OF THE WORLD. You asked for the discussion,
  2026-08-26:** *"after all I want to speak about the in game visuals - models/terain etc."* Opened as
  a placeholder for that conversation and **deliberately not designed here** — the same treatment
  `BL-45` got, and for the same reason: it is the one area where starting piecemeal produces work that
  has to be thrown away when the direction is set.

  What is worth having ready when we do talk, so the conversation starts from facts rather than from
  scratch:
  - **What the client draws today.** Capsules and coloured plates on a flat ground plane, with the
    3D/LoS work (`client-3d-and-los-design`) as the only shape decision ever made. Every creature in
    the game is the same silhouette at a different scale and tint, so a level-80 field boss and a
    level-3 wolf read as the same object — which is a presentation problem, not a content one.
  - **The two ground layers that already exist and could carry a look for free** — the totem and AoE
    decals (0.79.x) and the zone/region system, which already knows where every camp, town ring, road
    and dungeon mouth is. Terrain that follows the zones costs nothing in new data.
  - **The constraint that decides everything: it is a PHONE.** Model budget, draw calls, atlas size and
    APK size are the real ceiling, and the TMP atlas is already static and full at 250 glyphs
    (`tmp-font-atlas-is-static`). An art direction that ignores the device is a rebuild.
  - **The IP rule applies to ART as hard as it does to names** — see `naming-no-trademarks`. Silhouettes
    and skins that read as another game's creatures are the same problem the town names were.

  🟢 **OPENED AND ANSWERED, 2026-08-26/27. Direction set, step 1 built.** What you ruled:
  - **Low-poly stylised, CC0 sources**, accepted once it was clear it is swappable later — and 🔑 the
    thing that locks you in is **the RIG, not the polycount**: Unity **Humanoid** avatars mean a better
    body drops onto the same skeleton with no code change. Generic would be the rebuild.
  - **Downloadable assets** (*"a 100mb apk then download 10gb data"*) — yes, Addressables + a remote
    catalog off `UseStaticFiles()` on the server you already run. 🔴 **Not needed yet** (43 MB APK with
    zero art; low-poly lands ~60-90 MB) and ⚠ **bandwidth is the ceiling — your server is a phone.**
    The seam is in for free: models load by key through one function.
  - **Camera: unchanged for now.** *"Let's make proof of concept with models then see camera where it
    stands."* I had argued for pulling in to a 3/4 view — **deferred behind the POC, don't re-propose.**

  **Step 1 is BUILT (protocol 29, see the CHANGELOG):** `Category`/`Role` on the wire, the family→prefab
  fallback chain, facing + attack/cast/death animation off messages that already existed, and a
  "3D models: off" quality preset. Everything still renders as spheres until art lands — deliberately.
  ⤷ ✅ **DONE 2026-08-28 (0.100.1) — the Editor session happened and `humanoid.prefab` exists.** Every
  player, NPC and humanoid mob now has a body; the FBX source packs are committed beside it
  (`Models/Characters/`, `Models/Monsters/`), your call: *"push all ill later remove/update them to
  prefabs - if PoC works"*. APK 43 → **49 MB**.
  ⤷ 🔵 **NEXT, and it needs no code:** 50 of the 83 mob templates are not humanoid and wore a human
  body. **20 of them are fixed as of 0.100.2** (animal + insect, below); Undead (9) and Dragon (5) are
  the next two and their FBXs are already in the repo. The copy-and-paste table is in
  `docs/guides/UnityClient.md`, *"The nine monster names"*. Demon/Angel/Plant have no fitting model
  yet. ⚠ Monster FBXs stay on rig **Generic**; only bipeds get Humanoid.
  ⤷ ✅ **THE DEFERRAL IS REVERSED BY YOU, 2026-08-28 — AND BUILT (0.100.2):** *"can u add 1~2 mobs? U
  said u can do it alone as I don't have access to the pc. (again as poc)"*. The 2026-08-28 ruling
  (*"skip automating the prefabs for now .. then ill do 1-2 animals by hand"* — archived) assumed you
  would hand-make the first ones; you cannot reach the Editor, so the tool exists:
  **`Assets/Editor/ModelSetup.cs`**, run headless with `-executeMethod`.
  **`mob_animal` (Rat) and `mob_insect` (Spider) are in** — 20 of the 79 roster templates, and the
  first creature in the game (Ridgeback Pup, Lv 1) is one of them. 🔑 **They ANIMATE** — the monster
  FBXs ship Idle/Walk/Run/Attack/Death, which is exactly what `EntityView` has been driving since
  protocol 29, so the animation path is now proven with no new message and no client code.
  🔑 **Adding a family is ONE LINE in `ModelSetup.Families`** — `mob_undead` (Skeleton, 9 templates)
  and `mob_dragon` (Dragon, 5) are the next two and their FBXs are already committed. Say the word.
  What is still NOT automatable, and is why you held it: which model suits a family, and its height —
  both are authored per row, because the packs disagree on scale (the Rat imports 2.9 units tall).

  ⤷ 🔴 **THE PLAYER MODEL CANNOT ANIMATE — see `BL-102`. It is a missing FILE, not missing code.**

  Still un-started, in the order I'd do them: **terrain generated from the zone circles** (biggest
  perceived change per hour, needs no art) → creature families → **8 skill-FX archetypes** (one enum +
  colour on `SkillDef`; the client reads `SkillCatalog` directly, so no protocol change) → **~25 sound
  clips + 2-3 ambient loops** → skybox/fog/day-night (🔑 `GameClock` is already server-synced).

- `BL-102` 🔴 **THE CHARACTER MODELS HAVE NO ANIMATION CLIPS — I need a file from you, and it is the
  only thing standing between you and a running character.** You asked, 2026-08-28: *"now if we can add
  runing animation"*. The wiring is done and proven — the two mob families in 0.100.2 walk, run, swing
  and die off messages that already existed. The player does not, for one reason:

  **All 21 FBXs in `Models/Characters/` contain zero animation.** Measured, not assumed: mesh,
  skeleton, bind pose, 65 bones — and `AnimationStack` count **0**. The monster pack ships five takes
  per creature; the character pack you committed ships none. `humanoid.prefab` is a body with nothing
  to play, so it slides in its bind pose. No controller can fix that: there is nothing to put in it.

  🔑 **What you did right and what it buys you:** you set the character rig to **Humanoid**. That is
  the setting that makes clips *retargetable*, so any humanoid animation set drops onto these bodies —
  and onto the elf and demon bodies you add next week — with no per-model work. This is exactly the
  swappability argument from the direction talk, arriving early.

  **Two ways to close it, both free, either is fine:**
  1. **The pack's own animation file.** These packs normally ship a separate animations FBX beside the
     characters; it was not in what you copied across. If you still have the download, that one file is
     the whole fix.
  2. **Mixamo** — upload one character FBX, pick clips, download "without skin". CC0-safe for this use
     and the standard route for a Humanoid rig.

  **What I need is only the file(s) in `Models/Characters/Animations/`.** Then `ModelSetup` grows a
  character row and `humanoid.prefab` gets the same treatment the mobs just got — an afternoon, headless,
  no Editor session from you.

  ⚠ **Clip names matter less than the SET.** Idle / Walk / Run / Attack / Death is the whole
  requirement — those are the four parameters `EntityView` drives (`Speed`, `Attack`, `Casting`,
  `Dead`), and a missing one is silently skipped rather than an error. A cast pose would be a fifth and
  is the one the monsters do not have either.

- `BL-103` 🔵 **VISIBLE WEAPONS — the key shape is settled, the meshes are not. Your design, 2026-08-28:**
  *"if I make a sword1h.prefab and one sword1h_t20.prefab can that work? a t20 swords to be this one
  every rarity (we can change hue for example or glow) and if no tier prefab to fallback to default"*.
  **Yes — and `sword1h_t20` is not an invented name: it is literally an existing item id**
  (`TieredWeapons` emits `$"{w.Key}_t{L}"`).

  **The eight weapon keys** — `sword1h` · `sword2h` · `blunt1h` · `blunt2h` · `duals` · `bow` · `wand` ·
  `staff`. **The seven tiers** — 1 / 20 / 40 / 52 / 61 / 76 / 80.

  🔑 **KEY ON THE FAMILY + TIER, NOT ON THE ITEM ID.** The id space also holds `_rare` / `_epic` /
  `_legendary` copies (`ItemCatalog.QualityId`), `_lo` sets and `_dmg` variants — key on the id and
  every rarity demands its own prefab, which is exactly what your hue/glow plan avoids. The chain:

  ```
  Models/weapon_sword1h_t20     your tier prefab
  Models/weapon_sword1h         your default for that weapon
  (no file)                     draw no weapon; nothing breaks
  ```

  **Eight files give every weapon in the game a look**, and each tier prefab peels one rung off with no
  code change — the same shape that let `mob_animal` peel the animals off `humanoid`.
  🔑 The derivation is free and invents no taxonomy: **`WeaponType` + `IsMagicWeapon` maps exactly onto
  those eight keys** (Blunt+magic = wand, TwoHandedBlunt+magic = staff), and `ItemLevel` is the tier.

  **Rarity = a tint on whatever prefab answered** — your call, and correct. Two practical notes:
  - ⚠ **Prefer GLOW (emission) to hue.** Hue-shifting a textured mesh goes muddy; emission reads at
    phone size and does not fight the texture. Rarity is a 6-rung ladder and the drop copies populate
    all of it, so it is a natural intensity ramp.
  - 🔴 **It must go through a `MaterialPropertyBlock`.** `renderer.material.color` instantiates and
    mutates the shared asset — every sword in the world changes and batching dies. Same wall
    `SetOpacity` hit with models (the stealth-fade gap above).

  **Cost:**
  - ✅ **Free: the hand socket.** `GetBoneTransform(HumanBodyBones.RightHand)` on a Humanoid rig, no
    per-model setup — the second dividend of the rig choice.
  - ⚠ **Authoring, not code:** each mesh needs a consistent **grip origin and orientation** — the same
    class of decision as the family height in `ModelSetup`, and the same thing no script can decide.
    Duals need two sockets; a bow sits differently.
  - 🔴 **A protocol bump.** `EntityDto` carries NO equipment today. Four small values on the **spawn**
    DTO only (`EntityLean` untouched), the same shape `Category`/`Role` took in protocol 29.
  - ⚠ **Animation is per-weapon in a real MMO** and we have one clip set, so a sword swing will look
    right and a bow draw will not. Later problem, not a blocker.

  🔵 **TIMING — my recommendation, not a ruling:** there are **zero weapon meshes in the repo**, so like
  `BL-102` nothing draws until art exists. **Bundle this with the race bodies** — one protocol bump and
  one APK, instead of a reinstall now for a feature that draws nothing. The key shape above is all you
  need to start naming files against.

- `BL-104` 🔵 **THE WARRIOR'S SWORD-vs-BLUNT SPLIT — RULED BY YOU, NOTHING TO ATTACH IT TO YET.**
  Your ruling, 2026-08-29: *"the aoe warriors to be a 2h blunt while mele warriors to use 2h swords …
  we don't want an aoe warrior going with mace+shield and being even more hard to kill"*. This is the
  answer `classes_skills_csv/README.md` asked for when it flagged the split as *"new and nothing
  enforces it — say if it is meant as a rule"*. **It is a rule.**

  ✅ **The MECHANISM is built** (0.101.0, `WeaponHands` + `RequiredHands`) and the mace+shield half of
  your worry was already covered: Two-Hand Mastery has been two-handed-only since it was written, so a
  warrior who picks up a mace and shield already loses the entire passive.

  🔴 **What is NOT built is the split itself, because there is nothing to gate.** The 2nd-class warrior
  is ONE class and correctly takes either 2H type; the split lives at 3rd, and **there is no warrior
  3rd-class kit in the game** — `warrior 3rd.csv` and `war_aoe 3rd.csv` are both empty. So this entry
  is a **standing instruction for the day those files land**: every melee-warrior discipline passive is
  authored `AnySword + Hands.Two`, every AoE-warrior one `AnyBlunt + Hands.Two`. One line each, at the
  `WeaponMasteryProfile`. ⚠ Do not pre-invent the kit to have somewhere to put it (`BL-02`).

- `BL-106` ❓ **YOUR CROSS-CHAIN ID RULE — MEASURED. The masteries already obey it; six ids do not, and
  all six are the same class. Your call on each.** Your rule, 2026-08-29:

  > *"A chain of classes (fighter/mage) should replace their weaker skills with newer or continuing the
  > line .. but cross chain should have different id's … `mage_weap_mastery -> spellcaster_weap_mastery
  > -> buffer_weapon_mastery` and the other is `fight_weap_mastery -> war_weapon_mastery ->
  > swordmaster_weap_mastery`."*

  ✅ **It is now CHECKABLE**: `dotnet run --project tools/SkillCsvSeed -- --chain-audit`. It walks only
  the classes that really exist and reports ids learned by both chains, `Replaces` that cross chains,
  and defs whose declared `Class` disagrees with who is taught them.

  ✅ **THE GOOD NEWS FIRST — the weapon and armor masteries already read exactly as you describe.**
  Fighter chain: `fighter_weapon_mastery` → `tank_` / `warrior_` / `rogue_weapon_mastery`, with
  `fighter_armor_mastery` → `tank_` / `warrior_` / `rogue_armor_mastery`. Mage chain is separate
  throughout. **No mastery id is shared between the chains, and NO `Replaces` crosses a chain (zero).**

  🔵 **1. Two mage ids do not NAME their chain**, which is the half of your rule that is about reading
  the id rather than about collisions: **`weapon_mastery`** (the Mage's, `Skills.Mage.cs`) and
  **`armor_mastery`** (the Mage/Healer's). In your scheme they would be `mage_weap_mastery` and
  `mage_armor_mastery`. Nothing is broken today — the fighter's are prefixed, so they cannot collide —
  but a bare `weapon_mastery` is the exact ambiguity you are legislating against.

  🔵 **2. Six ids ARE learned by both chains, and every one of them is the WARCHANTER** — which makes
  sense, since the buffer is the mage that borrows from the fighter:

  | id | also learned by | what it is |
  | --- | --- | --- |
  | `tank_shield_mastery` | Tank, Bulwark | the Human buffer's Shield Mastery **is** the tank's skill |
  | `hp_boost` | Warrior, Ravager, Warlord | shared HP ladder |
  | `swap_atk_con` · `swap_atk_dex` · `swap_con_atk` · `swap_dex_atk` | most fighter classes | the ATK/CON/DEX stat swaps |

  ⚠ **26 more ids are shared and are NOT violations** — `shared 4th` (your own ALL-CLASSES block) plus
  the eighteen Sigils, which every ascended class learns on purpose. The audit separates them by a
  derived test, not a hand list, so a new sharer cannot hide among them.

  🔴 **THE COST, so you can price the decision: a rename here is NOT free like the class renames were.**
  A skill id is persisted in a character's learned set. Giving the buffer its own
  `buffer_shield_mastery` means minting a new id AND migrating every save that holds the old one —
  otherwise the skill silently disappears from those characters. That is why I have not done it.
  **Three options, pick per row:** (a) leave it — it genuinely IS the same skill, and sharing the id is
  honest; (b) rename with a load-time migration; (c) rename only the two bare mage ids (1), which is
  the cheapest and buys most of the readability.

  ⚠ **3. Thirty-seven defs declare the wrong `Class`** — e.g. every Sigil is `BaseClass.Fighter` but
  taught to both, `magic_proficiency` is Mage but taught to fighters. That field is not persisted, so
  fixing it is cheap; say the word and it goes in a sweep. It is listed by the same tool.

- `BL-105` ✅ **BUILT 2026-08-29 (0.101.1) — THE `WEAPON` COLUMN, in your grammar.** You approved it the
  day it was proposed and wrote the spec yourself:
  `weaponType1[|weaponType2|weaponType3][/hands]`, with `duals/1` a typo-warning and anything but
  `/1`/`/2` an error that invalidates the hands. All of it is live: the column is in all 24 files
  (1,425 rows, 187 with a real requirement), `--check` verifies every cell, and the grammar is
  documented in `classes_skills_csv/README.md`.

  🔴 **It corrected a semantic shipped hours earlier.** Your `sword|blunt|bow/1` includes a BOW — so
  hands narrow the **TYPES**, not the equipped weapon, and 0.101.0 had it the other way round. Fixed in
  `WeaponTypes.Resolve`; it made the code simpler, since the playtest-28 fold now falls out of the
  expansion instead of being a special case.

  Your authoring rule is recorded with it: *"passives won't ever be a (bow/duals or one handed weapon)
  … but if authored they should work that way"* — nothing is special-cased or refused.

  ⤷ The original proposal, for the record:

- `BL-105`.old ❓ **A `WEAPON` COLUMN FOR THE SKILL CSVs — my proposal, your call.** A skill's weapon
  requirement is real, enforced code, and today it is written **only in the free-text DESCR** (*"with
  2h sword/blunt"*, *"Require: Bow/Blunt"*, *"Blunt:"*). That means `--check` cannot verify it, and it
  is exactly how the elf's Combo Mastery bug survived: his CSV row said Bow/Blunt, the code said Blunt
  alone, and nothing in the repo could notice the two disagreed.

  **The proposal:** one more structural column, `WEAPON`, holding `type/hands` — `blunt/2h`,
  `sword|blunt/1h`, `bow`, `dual`, blank for none. The same move you already approved twice, for `AOE`
  (`BL-96`) and for `TARGET` (`[scope]/[breadth]`), and the checker gains a real comparison instead of
  a description it has to parse. ⚠ It touches the header of every file, so it is not something to do
  quietly on the way past — say yes and it is one increment.

---

## World & mobs

- `BL-48` ⏸ **Instances — you are holding.** Design is written (`design/Instances.md`). One
  load-bearing decision is still open: the daily attempt **GLOBAL vs PER-INSTANCE**. It changes the
  persisted model, so it is answered before anything is built. **Dungeons are the cheap half** —
  a dungeon is just a `SpawnZone` outside the town ring plus a teleport entrance, near-zero risk,
  and they can ship without instances.

- `BL-73` 🔵 **Mob social clans go back ON once the world map spreads the camps out** — your own note
  from playtest 23, *"Make a note to turn it on once the world map is in place."* The feature works and
  you saw it work; what makes it unplayable is **spawn DENSITY, not the 450 radius**: *"all mobs are
  spawning almost next to each other and hitting one wolf getting ganked by 10 other … For a mage lvl 9
  hitting a warefolf means dead."* Your target shape is *"it will call ONE, and while you fight, if
  others wander in the social range they will aggro"* — which is what the same 450 radius already does
  once a camp is not stacked on one point. **Nothing was deleted**: the twelve clans are still authored
  on the mobs and every line of the call code is intact, behind **one switch**
  (`GameConstants.MobClansEnabled`). Flip it when the camps are laid out; the retune that follows is
  the SPACING, not this feature.

- `BL-50` ⏸ **A boss/elite crafting-mat pile must obey the party loot rule.** Written as *(not
  tested)* and never tested. **PARKED with the rest of crafting** (see the top of this file) — it can
  only be verified inside the mat-farming playtest you have reserved.

- `BL-51` 🔵 **Castles + vault.** Needs the siege design first; consumes the reserved
  `VendorBuyTaxRate` hook.

- `BL-65` ✅ **BUILT 2026-08-13 (0.64.0)** — Hollow Crypt 39-42 / boss 44, **Sunless Warrens** 58-64 /
  boss 65, **Ashen Sepulchre** 80-85 / boss 90. Your layout exactly. 🔑 **The cause was real and not
  cosmetic:** a mob with a NATURAL level brings its own, so the spawner's band was only a label — the
  crypt was literally spawning 58 / 32 / 65 under a "44-48" sign. Fixed by the roster, not the sign.
  See `CHANGELOG.md`. Delete at the next sweep.
  - ⚠ The Sepulchre adds a **second 80-85 elite field**, which feeds the `EliteMatDrops` faucet — the
    top of the crafting ladder is now less scarce than `docs/balance/CraftingMats.md` measured. Ties
    into the farm-times decision you deferred under `BL-05`.

- `BL-68` ✅ **BUILT 2026-08-13 (0.64.0)** — nine new Stonewatch fields on a 3×3 grid east of the
  city, so every 16-40 band now exists **four times**. See `CHANGELOG.md`. Delete at the next sweep.
  - The **city was not moved**. You offered to; it turned out not to be needed, since the generator
    places a field by bearing + distance. Not moving it avoids relocating a town every player knows.
  - ⚠ **Stonewatch's gatekeeper now lists 12 fields.** A long menu on a phone — the same question
    `BL-41` asks about the craft page, in a different window.

- `BL-70` ✅ **BUILT 2026-08-13 (0.64.0)** — mob clans + the rogue's `Lure`. Twelve clans authored on
  the name-root families, a 450 radius, damage-only trigger, and a no-damage mob-only taunt whose
  ladder is reach (200/400/600). See `CHANGELOG.md`. Delete at the next sweep.
  🔴 **`Lure` MOVED 2026-08-19** — it was the 2nd-class rogue's at 20/28/36 and is now the melee/DUAL
  3rd's at **40, level 1 only** (*"No lure for lvl 29 and below .. It's a skill that need the prawl
  effect"*). Levels 2-3 are unreachable until you place their rungs in `dual 3rd.csv`.
  ⚠ **Untested against a real camp** — it needs a playtest in an orc/mantis field to say whether 450
  and "the answering mobs don't cry in turn" give the fight the size you pictured.

- `BL-78` 🔵 **MOBS ARE TOO EASY — three of the four items are BUILT; only THE BILL is left, and it
  is yours to rule.** Your playtest-25 words: *"now mobs as general feel easy ... tank get hit fo 30 ..
  others for 100-200 but the rogue almost one blow it .. mage one/two shot it .. and there is no
  thrill in fighting"*. The research you asked for is
  **[balance/MobCurveVsIG.md](balance/MobCurveVsIG.md)** — 2,831 IG creatures, levels 1-83.
  1. ✅ **THE HP MULTIPLIER — BUILT 0.94.0, and you moved the lever while ruling it.** This entry used
     to say "author `MobMod.Hp` across the roster". You ruled instead (2026-08-27): *"the 15k mobs are
     zone placed with x2/x3 hp .. some zones can have x1"* — so the **ZONE** carries it, the same
     creature reads ×1 in one field and ×3 in another, and not one template was edited. One derived
     ladder (`WorldPlan.HpScaleFor`): ×1 below 40, ×2 from 40, ×3 from 61, overridable per field with
     `Band.HpScale`. `MobBaseStats.Hp(80)` = 5,160, so ×3 = **15,480** — your *"the 80 mobs should
     have 15k not 5"* on the nose. ⚠ A boss ignores it (0.89.0's measured 12-25 min band derives from
     the same curve); an elite does not. It multiplies HP and nothing else.
  2. ✅ **A CASTER MOB IS NOT A SQUISHY MOB — BUILT 0.94.0.** *"caster mobs are not weaker than the
     other, they just use spells (and have a bit less pdef, evasion not twice less)"*. ⚠ **This entry
     was wrong about the cause and said so for days**: it claimed a caster paid twice with "low P.Def
     AND low HP", and `MobRole.Mage` never touched HP at all. The double-dip was on DEFENCE — the
     role's ×0.7 compounding with a template's own `MobMod.PDef`, worst at `watcher_eye` (0.5 × 0.7 =
     **×0.35**). The role now reads like Archer, ×0.85 and +8 evasion — IG's `Light Armor Type` word
     for word.
  3. ✅ **THE PLAYER CURVE — BUILT 0.91.0.** Max HP is a growth rate that steps at every class change,
     keyed by discipline. A robe class at 52 survives **21s, not 9s**. See `BalanceMatrix --hpcurve`.
  4. 🔵 **THE BILL FROM 0.73.0, AND IT IS STILL YOUR CALL — now with a second charge on it.** Doubling
     creature defence took a full S-grade character from **347 to 603 farm hours** and dropped an elite
     camp from 115% of a normal farm to **76%**; an unattended farm at parity stopped sustaining itself
     (level 52: 26 kills before the HP bar empties, now 9). ⚠ **0.94.0's HP ladder adds to this bill,
     not beside it** — a ×2/×3 field is ×2/×3 the time-to-kill for the same reward, so `BL-22`'s budget
     and the auto-hunt consumable question both move again. Nothing has been retuned to compensate,
     deliberately: you asked for the mobs to be heavier, and quietly paying for it out of drop rates
     would hide whether the change worked. **Measure it with `--goldflow` and `--guards` before ruling.**

- `BL-79` ✅ **TOWN / FIELD GUARDS — BUILT 0.94.0, both halves, nothing outstanding.** Two tiers
  (**town = level 80, S grade Epic +0**; **field = level 90, S grade Epic +16, War Rune**), eight posts
  (five city gates, three quiet farming fields), karma-keyed aggro so only a PK is acquired, PvP-on as
  the gate to attack one, **no karma / flag / exp / drop** for the kill, aggro 400 melee / 600 archer,
  respawn **75±15s** town and **1.5±0.5s** field. Your *"the npcs still refuse trade"* shipped with it:
  `NpcRefusesService` on ten handlers, so killing the watch buys a PK the SAFE ZONE and nothing else.
  🔑 **Your fork — "treat them as mobs" vs "treat them like a player" — resolved to the player route**,
  because your calibration target is a player. `MobBuild.LearnsKit` teaches the PASSIVE half of the
  class kit; the town pair carries **no invented multiplier at all** and mirrors the reference player
  (P.Atk 1,158 vs 1,214; P.Def 1,101 vs 1,101). Only the FIELD pair has a passive, `GuardTower`, for
  your *"almost 1 shot a pk"*.
  ⚠ **Widening past eight posts is one line** (`GuardedFieldIds`, and the one-post-per-city loop),
  held until you have played them.

- `BL-80` 🔵 **FORTRESS SIEGES — your own design, transcribed whole, and you said it can wait.** *"this
  system can be defered and just have it as idea or can build some base ground for it."* Recorded here so
  it is not lost; the verbatim text is in
  [Playtest-Archive.md#playtest-25](testing/Playtest-Archive.md#playtest-25). The shape:
  - **A weekly window.** All fortresses attackable once a week; the quest is offered **30 minutes before**
    the start. *"once defeated they cannot be reengaged"* — no respawn, no re-taken quest.
  - **A garrison of social pMobs on a ±2 band** (a Lv 60 fortress runs 58-62): troopers/tanks and archers
    on **basic attack only** in **common t52** (aggro 400, archer range 600), mages in common t52 that
    cast, healers in common t52 that heal allies and deal no damage (passive, heal range 500, **normal
    heals not quick**). **Commanders** in **rare t52** use skills; the **king's guard** is **rare t61**
    (archer, mage, two healers, and a tank if the king is a warrior or a warrior if the king is a tank);
    the **king** is **mythic t61** with a War Rune and **twice HP/pDef/mDef/pAtk/mAtk**.
  - **Four gated stages through one entrance:** 10-15 outer troops → an outer **mob-gate** → 20-30 troops
    inside → the commander party → an inner gate → the leaders and the king. A **gate** is a *"targetable
    imovable door"* that becomes mortal only when its side is cleared, **immune to skills, DoT, debuffs and
    crits**, takes ~1000 normal attacks (his suggestion: 1 damage per hit, ~1000 HP).
  - **The commander party fights like players** — *"kill the healer 1st idea"*: near-infinite MP, quick
    heals, party heals, debuff removal; the tank taunts and uses an ultimate; the others stun and debuff.
  - **PvP is automatic inside the field**, other parties and clans can attack the same fortress, the king
    drops **boss loot + raid points**, and the completion quest pays *"every participant (not all that took
    the quest - but who fought inside)"* in gold, EXP and raid points.
  - 🔑 **It is a template.** *"if we make a template of a fortress - we can reause it just change the grade
    of equipment"* — so one authored fortress plus a grade parameter is the whole content pipeline.
  - 🟡 **Gated on real prerequisites, which is the honest reason to defer it**: `BL-47`'s pMobs (built),
    `BL-51` castles/sieges (nothing exists), **raid points** (no such currency), a **weekly world clock**
    (`GameClock` has no weekly window), and mob **healer/commander AI** that casts like a party. ⚠ It also
    presumes clans, which are **OFF** (`BL-73`).

- `BL-52` 🔵 **World expansion toward 1kk+.** The 0.33.0 re-layout was the first step and nothing
  followed it. `BL-21` is queued behind this one.

---

## Quests

*(`BL-54` and `BL-55` were closed on 2026-08-27 — both were already true. The tutorial hands the
newbie boxes out on its level-10 and level-15 steps, and the newbie light/robe sets ARE the two real
starter sets, not placeholders. See [BacklogArchive.md](BacklogArchive.md).)*

---

## Admin & debug tools

- `BL-56` ✅ **BUILT 2026-08-15** — the Equip tab is one page with three selection boxes (type /
  quality / tier) instead of a drill-down. 🔑 The cause was worth knowing: it could only ever hand out
  **Mythic**, because the authored piece IS the Mythic one and the lesser qualities are generated copies
  at suffixed ids — so five sixths of the gear ladder was unreachable from the window used to set up a
  test. Chips rather than a dropdown, per your *"whichever is easier"*. See `CHANGELOG.md`. Delete at
  the next sweep.

- `BL-66` ✅ **BUILT 2026-08-13** — the item-id reference and the staff-only id row. Kept here for one
  release only because it is the thing that unblocked his own §75/§76 testing; delete at the next
  sweep. *"Need a grouped list (in a file - like the commands one) with each equip/item ID, and in
  each items details in game only for admin to see: a row like the enchant info one with the ID."*
  → `docs/guides/ItemIds.md` (1,078 ids, **generated** by `tools/ItemIds`, never hand-written) and an
  `id <defId>` line under the enchant line on every item card, staff only.

- `BL-82` ✅ **BUILT 2026-08-23 (0.80.0) — BOTH halves, not just the badge.** Playtest 25: *"Add a flag
  for admin to see that he is in god/invis ... but now i cannot see nothing."* Kept here for one release
  because it is the item you asked after by name; delete at the next sweep.
  - **The badge** — top strip, beside the version: your rank, plus `GOD`, `INVIS` and any forced speed.
    Red background for god, indigo for invisible. Staff only, and read from the SERVER's view of you, so
    a demotion that clears god mode clears the badge too.
  - **The opacity**, exactly as you ruled it: **0.7 stealthed** (Prowl / Conceal / Shrouding Hymn) and
    **0.4 invisible** (`/invis` and the rogue's Vanish both), on **your own marker only** — plus the
    **golden ring** around a god admin. 🔑 *For themselves only* is enforced by there being nothing to
    leak: the push describes one connection's own character and no one else's, and the observer half was
    already true server-side (`BL-69` — a hidden entity is an OMISSION from the snapshot, never a flag
    the client is trusted to honour).
  - 🔑 Why it had been silent: the god badge existed in the **WPF harness** and died with it in 0.42.8.
    The server never stopped pushing the state — the Unity client simply had no handler, so it went into
    the void, and `/invis` never pushed at all. `AdminStateDto` is now `SelfStateDto`, three fields
    richer, and it is pushed **on change from the tick loop** rather than from each command that could
    cause one: hide ends by expiry, damage, acting, a flare and death, and missing one of those would
    leave a visible character faded. Protocol **24 → 25**; see `CHANGELOG.md`.

*(`BL-86`, the shutdown countdown, was closed on 2026-08-27 — *"this is good enough … noticable
enoght"*. The toast is accepted; red text is optional and owed to nobody. See
[BacklogArchive.md](BacklogArchive.md).)*

- `BL-72` 🔵 **Unbuffed auto-farm is not survivable for either damage kit.** His `0a` note
  (playtest-22): *"they both have hard time to farm without buffs .. when i login in 1-2h after the
  npcs buffs are gone both are dead and with potion buffs."* Two separate questions inside it, and
  the second is the real one:
  1. Is an unbuffed nuker/champion *meant* to survive an unattended hour? The NPC buff ladder is
     currently load-bearing for auto-farm, which nothing was designed to be.
  2. **It also invalidates the `0a` measurement itself** (`BL-18`) — a run that ends in a death an
     hour in is not measuring the kits, and the auto-buff tab (§78) is what would keep one alive long
     enough to measure. Read the two together before spending a session on either.

---

## Housekeeping

- `BL-59` ✅ **BUILT 2026-08-14 (0.66.0)** — your TARGET-based re-spec, all three parts. Single-target
  support of a non-party player is allowed only while they are clean; a pvp/pk player can be supported
  only from inside their party, and doing it **flags you**; party invites are unrestricted; trade is
  blocked for **pk only**, not for a purple flag; res in party works for both. The Ultimate Scroll of
  Resurrection is tradable (the tutorial's copy is the separate `_bound` clone). ⚠ This **opens**
  something that was shut — support used to be party-only. Old self-based text in
  [BacklogArchive.md](BacklogArchive.md). See `CHANGELOG.md`. Delete at the next sweep.

- `BL-60` 🔵 **Death penalty, resurrection skills, Angel's Protection.** The 2026-07-17 design —
  death XP penalty, res skills and scrolls, a buff-keep-on-death. Nothing exists in code. Overlaps
  `BL-59`; read them together.

- `BL-61` ⏸ **Network payload optimisation.** Split/delta snapshots and a local buff countdown, then
  optionally MessagePack. Deferred deliberately: no measured problem, the protocol still churns
  every session, and MessagePack's dynamic resolver does not work under Unity/IL2CPP without a
  codegen step. A late, one-line swap once the protocol settles.

- `BL-62` ⏸ **Bot-prevention CAPTCHA** ("petrification" after 200-500 manual kills). Revisit with
  behavioural detection. Your own worry stands: an AI, as opposed to an if/else bot, solves it.

---

## What was closed on 2026-08-12 and is deliberately NOT in this file

The playtest-21 batch and `58d` shipped in `267313d` → `ed75bac`: shields option 3 (P.Def ÷5,
Shield Mastery ×5) · the shield enchant `+9 → +3` · the wood/iron shield block profile · the whole
start quest re-spec · training club and knives deleted · the `x500` mats stall · auto-farm ignoring
`RequiredWeapon` · the training dummies + rank titles · `65d` · `67i` · `68h` · `63i` · `62j` ·
broken jewels → 9/5/3 · **item tags and the full `/give`**. They live in `CHANGELOG.md`.

**The housekeeping batch, later the same day** took out `BL-37` (the test heal, deleted — and the
retired-skill-id leak it exposed in the save loader) and `BL-58` (`58i`, the inspiration-game name
purge; the tag is `IG`).

## Closed on 2026-08-26 by your own ruling

**`BL-47` — YES, AND IT SPLITS THE WORK IN TWO.** Your answer, verbatim: *"yes. Player mobs are hand
crafted and field mobs stay on curve. Player mobs are player stats with equipped real items. Pk guards
with overechsnted gear and fortress fighting npcs with undergear as we described."* So: ordinary field
creatures keep the `MobBaseStats` curve with `MobMod` passives — one function moves every creature,
which is the property you did not want to lose — and player-built creatures become a **hand-placed
content tool** with real player stats and real worn gear. Everything built in 0.70.0 serves that shape
unchanged, and **`BL-79` (PK guards, over-enchanted) and `BL-80` (fortress NPCs, under-geared) are now
the roadmap for it**, both with the gear direction you just named. Old text in
[BacklogArchive.md](BacklogArchive.md).

**`BL-49` — LEAVE IT.** *"well ofc it's lot slower to llv up 85+ than 20... Leave it."* The 125%-of-a-
level-at-20 against 0.1%-at-85 spread is the EXP curve doing what you want it to do. Closed; not
re-proposed. ⚠ One consequence to watch in play rather than in the file: `BL-13`'s boss HP curve made
the level-44 boss take ~3× longer and boss exp is derived from kill time, so it pays ~3× more — about
half a level per head in a nine-man. Nothing caps it; the sanity rail only bites below level ~37,
where nothing spawns. See the 0.89.0 entry of `CHANGELOG.md`.

---

## Closed on 2026-08-14 by your own later ruling

**`BL-26` (the vendor half of the buy-back design — "a longer sold list") is DELETED, not built.** It
descended from the *old* design recorded at `Roadmap.md:126` (*"a buy-back menu — last 10
deleted/sold"*). Your **`M14`** ruling in playtest-19 replaced it — *"cap the vendor buyback list at
10-15 items"* — and that is what ships: `GameConstants.BuyBackSlots = 12`, alongside a **separate**
5-slot `Restorable` list for bin-deletes (`C18`), which is the better shape you yourself proposed.
Newest ruling wins, so lengthening the list now would walk back your own number. Old text in
[BacklogArchive.md](BacklogArchive.md).

---

**Six more were checked out against the CODE, not the list** — every one was already built in a pass
whose commit carried no changelog entry, which is why they were still sitting here: `BL-31` (`55b`,
the HP price on a skill card), `BL-32` (`55c`, refusing an HP skill at low HP), `BL-33` (`57b`, the
duplicated Robe Armor Mastery), `BL-53` (Elder Marius's empty "!"), `BL-63` (Frost Bind stripping a
mob's HP multiplier) and `BL-64` (the target dropped for a physical cast). The table in
`CHANGELOG.md` names the code that proves each one. ⚠ `BL-63` and `BL-64` were closed on a **reading
of the code**, never re-tested by him — they are on the checklist as verifications, not called done.
