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

- `BL-10` 🔵 **A floor under the fading bow-caster penalty.** The bow penalty currently vanishes
  entirely when you punch down. You were asked whether you want a floor under it and the reply is
  still empty. *(playtest-21 `64e`.)*

- `BL-11` ✅ **BUILT 2026-08-14 (0.65.0)** — the mob layer gains an **mRes channel**
  (`MobMod.MagicResist` + a *Magic Resistance* mastery track, the CSV's own "???? Resistance" row
  filled in), and the pair is actually authored: **Warded** (P.Def ×0.8 / M.Def ×1.5 / mRes +20%) on
  Grave Lich, Aether Wisp and Spiteful Ghost; **Ironhide** (P.Def ×1.5 / M.Def ×0.8 / mRes **−20%**,
  a real magic WEAKNESS) on Shield Skeleton, Fomor Brute and Dread Knight, plus a Magic Resistance
  rung on Obsidian Knight's Stoneplate. Before this, one mob in the game was anti-magic and none was
  anti-physical. See `CHANGELOG.md`. Delete at the next sweep.

- `BL-12` 🔵 **Enchant bonus should scale with what you put in.** Your objection to the flat-offset
  ruling, unanswered: *"not a warrior invest +3 and gets the same bonus as cleric +16."* Today the
  offset is identical for every class, by grade. Needs your call before anything moves.
  *(playtest-21 `68e`; the 0.60.0 model is in `docs/balance/BalanceMatrix.md` §E.)*

- `BL-14` ✅ **BUILT 2026-08-14 (0.65.0)** — two of your three clauses were already true (a mob's
  attack SPEED and CRIT RATE have come off `InnateWeaponType` since 2026-08-10); the third was not.
  `MobWeaponPowerFactor` (`433 / weaponBaseSpeed`) gives a mob the per-hit power a player gets free
  from the weapon ITEM, so a slow weapon buys damage instead of being a pure nerf. Measured at 40:
  Dual 171 P.Atk / 13.2% crit, Blunt 195 / 4.4%, 2H 227 / 8.8% — and **DPS is flat across all of
  them**, which is what makes it a trade. ⚠ **BOW is ×1.00 on purpose**: `MobRole.Archer` already pays
  that trade explicitly (P.Atk ×2, 450 range), and doubling it would put an archer at ~3× per arrow.
  See `CHANGELOG.md`. Delete at the next sweep.

- `BL-15` 🔵 **`precision` / `anti_magic` floor rungs should follow the CLASS CHANGE, not level 76.**
  Implied by your rogue ruling and never carried back into either checklist — recorded in the
  changelog as "owed back to him" and then dropped. Confirm and it is a small authoring change.

- `BL-16` 🔵 **Heal powers need re-authoring — and they are YOUR numbers, so you have to move them.**
  They sit at ~151-301 against a scale that has moved to ~1000. ⚠ **Re-marked 🔵 on 2026-08-14**: the
  ladder is authored in `docs/data/classes_skills_csv/cleric 2nd.csv` (two of his renames on 2026-08-17,
  content untouched: `healer 20-35` → `cleric 20-35` → `cleric 2nd`) — *"heal with power 151 / 195 /
  245 / 301"* on Heal and Quick Heal at learn levels 20/25/30/35, and 121/156/196/241 on Party Heal.
  Raising it in code is a CSV retune, which your own rule forbids, so it was left alone (your call,
  2026-08-14: *leave the CSVs alone*).
  🔑 **Measured, so the size of the gap is known:** a group buff learned at 35 is `35 × 20 × 9` =
  **6,300**; Quick Heal L4 is `301 / 2s × 10 × 1` = **1,505**. That is **~4×**, against the ~1.3× you
  sized `BL-71` for. **Landing your ratio needs Quick Heal ≈ 970 power** — which is exactly the "~1000
  scale" this entry names. Two ways out and both are yours: send new 20-35 numbers, or let the 40+
  rungs (`BL-02`) carry it, since a ~1500-power quick heal is a 40+ rung by your own sizing.

- `BL-17` 🔵 **Re-author `BuffMagAtk`, and give magic-only buffs an explicit magic %.** ⚠ **Re-marked
  🔵 on 2026-08-14**, same reason as `BL-16`: the healer CSV authors `Force` at 25 as **M.Atk x1.55**
  and Frenzy as **mAtk x1.1**, so this is a retune of your own data.
  ⚠ **And there is a discrepancy to settle first:** your CSV's Force@25 is `x1.55` while the shipped
  `FamMagAtk` rung is **+25%**. Per your `xN.NN`-is-a-percent convention those may not even be the same
  claim. Not reconciled by guessing — say which is right.

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
  - ⚠ **Your buff:heal ratio does not hold yet, and the buff side is not the reason.** You sized it
    against "a quick heal with ~1500 power at that lvl"; the cleric's heal ladder actually stops at
    skill level 4 — learned at 35, power 301 — so a group buff currently out-threatens a heal by ~8×
    instead of ~1.3×. That is **`BL-16`** (heal powers "sit at ~151-301 against a scale that has moved
    to ~1000"), and it is the half that has to move.
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
  are 5 different ork types and I go there for lvl up, and several different settlements and zones
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

- `BL-23` 🔵 **The coin curve.** Gear value follows the tier ladder while coin stays linear, so the
  gap drifts to **51×** by level 76. The note in the archive is explicit that *"the real fix is the
  coin curve, not another multiplier"* — every rate tweak since has been a patch over this.

- `BL-24` 🔵 **The enchant-scroll types — you wanted to discuss them.** *"ENCHANTS — you said you
  want to DISCUSS them … bring it up when you are ready."* The three types (breaks / −1 / safe) ×
  six grades shipped in 0.53-0.60; the conversation you asked for never happened. The 30× drop cut
  (`62j`) is ratified and stays.

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

- `BL-28` ⏸ **MP potions** — held until the 40+ kits decide the MP economy.

- `BL-29` ⏸ **SP bottles** — 1e9 SP → one bottle; also what keeps `SkillPoints` an `int` honest.

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
  - 🔴 **STILL OPEN — the nuker 3rd values have no code to attach to.** Arcane/Frost/Pyro Burst ×1.5,
    Frost Spikes ×0.7, Frost Pierce ×0.5, Witches Curse ×0.7, Witches Scarecrow ×0.5, Arcane Void ×0.3 are
    authored and waiting on the kit. `nuker 3rd.csv` also earns its line in `Check.Specs` that day.
  - 🔴 **STILL OPEN — Snare Trap and the Warchanter's stun-rider** are hybrids that already exist and are
    deliberately left at ×1. Retro-taxing a built class is your call; one line each.
  - 🔴 **STILL OPEN — "buff removeal" has nothing to tag.** Dispel Magic was deleted 2026-08-07. Arcane
    Void ×0.3 is the cancel you have authored, and it lands with the nuker kit.
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
    human ftr ×0.94/×0.78, elf mage ×0.85/×0.56, human mage ×0.78/×0.39, ork mage ×0.72/×0.29.
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

    | spell | dmg | % of HP | ×1 | **×2 (ruled)** | ×5 | ×10 |
    |---|---|---|---|---|---|---|
    | Frost Spikes | 160 | 12.5% | 10.8% | **21.6%** | 53.9% | 100% |
    | Frost Pierce | 160 | 12.5% | 10.8% | **21.6%** | 53.9% | 100% |
    | Elemental Blast | 270 | 21.0% | 18.2% | — | 91.0% | 100% |
    | Thunderstorm | 541 | 42.2% | 36.5% | — | 100% | 100% |

    Your ×10 guess came from expecting these to be small hits; they are not — a mage has the smallest HP
    pool in the game, and ×10 on either Frost skill is a guaranteed cancel, i.e. Disrupt rather than a
    nuke. At ×2 they read 21.6% per hit (9.9% through Resolve) and fire every ~2.5s, so they compound
    into roughly a third of a 4s cast.
  - 🔴 **×2 IS NOT IN THE CODE YET, and cannot be:** the `nuker 3rd` kit is unbuilt, so neither skill has
    a `SkillDef`. The ruling lives in the CSV and here. **When the kit lands: set
    `SkillDef.InterruptMult = 2f` on both, delete the literal table in `BalanceMatrix`, read the
    `SkillDef`s instead, and give `nuker 3rd.csv` its `Check.Specs` line.**
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
  - 🔴 **THE ORK BUFFER SHOULD CARRY MORE — undecided.** *"buffer ork should have more but yet not
    desided - note it"*. No number invented; it arrives with his authoring.
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

- `BL-75` 🔵 **The heal-at-0 skill wants a warrior/ork home.** Playtest 23, on the old Undying Will
  behaviour: *"That idea for undying skill is good for a warrior ork, when he must die just heal himself
  30%"* — and *"as I said good skill for a warrior"*. 🔑 **It is already built and needs no new mechanic**:
  `LastStand` (`SkillEffect.LethalSave`, revives to 50% of max HP off a fatal blow, buff consumed) has
  been in the catalog the whole time; its learn line went in the 40+ purge. What is missing is only a
  **class + a level + the percentage** — which is 40+ authoring, so it waits on `BL-02` with everything
  else. Your two words to settle when you get there: is it Ravager/Warlord or race-gated to the ork, and
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
  ⤷ 🔴 **OWED: one Unity Editor session** (import, Rig=Humanoid, save as
  `Assets/Resources/Models/humanoid.prefab`) — the steps are in `docs/guides/UnityClient.md`,
  *"Dropping in a model"*. **A version bump and a new APK go with it.**

  Still un-started, in the order I'd do them: **terrain generated from the zone circles** (biggest
  perceived change per hour, needs no art) → creature families → **8 skill-FX archetypes** (one enum +
  colour on `SkillDef`; the client reads `SkillCatalog` directly, so no protocol change) → **~25 sound
  clips + 2-3 ambient loops** → skybox/fog/day-night (🔑 `GameClock` is already server-synced).

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

- `BL-78` 🔴 **MOBS ARE TOO EASY — the DEFENCE and ATTACK halves are BUILT (0.73.0); what is left is
  authoring, and one bill.** Your playtest-25 words: *"now mobs as general feel easy ... tank get hit fo
  30 .. others for 100-200 but the rogue almost one blow it .. mage one/two shot it .. and there is no
  thrill in fighting"*. The research you asked for was done first and is
  **[balance/MobCurveVsIG.md](balance/MobCurveVsIG.md)** — 2,831 IG creatures, levels 1-83, read with
  their NPC skill lists off `l2elo.com`. It found `MobBaseStats` had been fitted to an **older chronicle
  of IG** (the same creature id reads ~3× lower there), that the gap was **defence and attack, not HP**,
  and that IG authors creatures exactly the way `MobMod`/`MobMasteries` does — its tier words measure
  ×0.82 / ×1.00 / ×1.21 / ×1.61, which is our own `DefTable` ladder. **Shipped 0.73.0:** P.Def, M.Def,
  P.Atk and M.Atk refitted to the current chronicle as one smooth `a·(level+shift)^k` each (your
  bosses/instances constraint — no floor, no band, no kink anywhere), ~×1.9 defence and ~×1.65 attack at
  the top, level with the old curve at level 1. What is left:
  1. 🔴 **THE HP MULTIPLIER, AUTHORED ACROSS THE ROSTER — this is your *"the 80 mobs should have 15k not
     5"*, and it is NOT a curve change.** Measured: 77% of IG creatures are tagged `HP Increase (1x)`,
     23% carry ×2-×5. Base HP at 76 is 4,298, so ×3 = **12,894** and ×5 = **21,490** — your 15k and your
     21k, both, and you worked that out yourself. `MobMod.Hp` already exists and already works; we use it
     on a handful of creatures where IG uses it on a quarter of them. So the job is choosing which
     creatures read as dangerous, not moving the lever `BL-47` warned about spending. ✅ The base HP
     shape was left alone on your ruling — it measures 0.87 → 1.08 of IG's from 40 up.
  2. 🔴 **A CASTER MOB IS NOT A SQUISHY MOB** — *"caster mobs are not weaker than the other, they just use
     spells (and have a bit less pdef, evasion not twice less)"*. The caster archetype currently pays
     twice (low P.Def **and** low HP) for a role that should cost it a little P.Def and nothing else.
     ⚠ **This is IG's own rule, word for word**: its caster tag is `Light Armor Type` — *"Weak P. Def.
     and strong Evasion"* — which costs defence, buys evasion, and does not touch HP.
  3. 🔵 **AND IT MAY BE THE PLAYER CURVE TOO** — *"a healer with 1500 hp getting hit for 300 is abit harsh
     .. one time less defence cuz of robe the second hinder is the amount of hp"*. A robe class paying
     for its role twice, on the player side, is the same complaint as (2) pointed the other way. Decide
     these two together or a healer ends up in the same hole a caster mob just climbed out of.
     ⚠ **0.73.0 made this louder, not quieter** — creature attack rose ~×1.65.
  4. 🔴 **THE BILL FROM 0.73.0, and it is your call.** Doubling creature defence doubles time-to-kill, so
     a full S-grade character went from **347 to 603 farm hours** and an elite camp fell from 115% of a
     normal farm to **76%** — `BL-22`'s budget has to be re-solved against the new numbers. An unattended
     farm at level parity also stopped sustaining itself (level 52: 26 kills before the HP bar empties,
     now 9), so auto-hunt at parity now needs consumables. ✅ The same change put field bosses inside
     your `BL-13` band (17-26 min) without touching a boss.

- `BL-79` 🔴 **TOWN / FIELD GUARDS — the first real use for the player-built creatures, and `BL-47`'s
  ruling of 2026-08-26 makes it THE use.** *"Pk guards with overechsnted gear"* — so the guard's power
  comes from what it WEARS, hand-placed and few, which is exactly the shape you approved. Your design,
  playtest 25, verbatim: a **Lv 80 mob in Mythic t80** that is *"only aggressive
  thowards PK (ignores mobs/pvpOrNormal-players)"*, aggro **400 melee / 600 archer**; *"ofc if u hit them
  (pvp-on) they act as passive mobs"*, plus a **PK radar**. *"each towns exit will have two guards - a
  tank and an archer, they dont use skills (only normal attack) but can have rune_war (unlimited)"*, and
  *"they can have a class with passives"*. Last clause: ***"a pk cant use npcs"*** — even if he kills the
  guards and gets inside.
  🔑 **Almost every part already exists**: 0.70.0 builds creatures through the player pipeline with real
  gear and a held War Rune, karma/PK state is tracked, aggro range is per-template, and hand-placement is
  the `MobType.HandPlaced` fence. What is genuinely new is **a target filter keyed on karma** and **an NPC
  lockout for a PK**, and neither is large.
  🔵 Open: does a guard respawn on a timer, and does killing one carry a karma or PvP consequence of its
  own? Not stated.

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

- `BL-54` 🔵 **Newbie items through quests** — hand the starter weapon/armor/jewel boxes out at
  levels 6/8/10. Your plan, never scheduled. ⚠ Re-check it against the tutorial as it now ships
  (`267313d` moved every box onto the step that needs it) before building.

- `BL-55` 🔵 **Two real starter armor SETS.** The current newbie light/robe sets are placeholders
  waiting on your numbers.

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

- `BL-86` 🔵 **THE SHUTDOWN COUNTDOWN IS TEXT, NOT A BIG RED BANNER — your call whether that is enough.**
  `/server shutdown|reboot|on` is **BUILT** (0.78.0) with your whole announcement ladder — hours, then
  10-minute steps, then minutes, then every second for the last 60. What it is NOT yet is your
  *"onscreen/chat message - red big"* and *"its permanent on the screen 60..59..58"*: every line goes out
  on the existing `Notice` toast plus System chat, which fades after a few seconds and is drawn in the
  ordinary toast colour. That was deliberate — the toast needed no protocol change, so the whole feature
  works on a client built before it. Making it a red, large, and (under 60s) persistent overlay is a
  client-side element and a new push. **Say if the toast reads well enough**; if not, this is small and
  rides the next client batch with §89's three UI changes.

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
