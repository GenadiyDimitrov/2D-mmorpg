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
gone) are **built and deleted**, and `BL-49`'s boss-EXP half is built with the levelling-curve half
left open on its entry. `BL-45` (the presentation pass) is **his own "separate discussion later on"**.

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
loses the global curve lever**, which is now the open question on that entry. Every built row in that pass
came back green; the UI polish it asked for lives in `testing/Open-Checklist.md` §89.

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
(the Mindwriter's misleading `(cost …)`) out with them. See `CHANGELOG.md`. Two are left.

- `BL-02` 🔵 **The 40+ class kits (3rd and 4th tier).** Blocked on your skill CSVs —
  `docs/data/classes_skills_csv/` holds nothing above level 35. Still the single biggest content
  unlock in the project; nothing is invented in the meantime, by your own rule.

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

- `BL-13` 🔴 **ANSWERED IN PLAYTEST 25 (2026-08-16/17) — A BOSS IS 10 TO 30 MINUTES, and the target
  RISES.** Your ruling verbatim: *"not nececerily the 20lvl should take 6mins ..but they can take a lot
  more ... It's a Boss the bosses should take 10-15 even 30 mins to kill (depending on the gear). It
  should feel hard but rewarding .. A 3 min boss is not a boss its a stronger elite mob .. Bosses should
  have stronger defences,more atk (not one shooting but a tank can feel it), A healer,tank and dds in a
  party are a must ..."*
  🔑 **This reframes the measurement completely.** The 11× spread I reported is not the defect I called
  it: **600-1800s is the band you want**, and the measured curve is wrong at the BOTTOM, not the top.

  | Lvl | boss HP | 3-DD dps | measured TTK | vs your 600-1800s |
  |---|---|---|---|---|
  | 20 | 36,000 | 448 | **80s** | **7.5× too fast** |
  | 40 | 132,000 | 446 | **296s** | 2× too fast |
  | 60 | 292,000 | 427 | 684s | **inside the band** |
  | 76 | 466,000 | 525 | 888s | **inside the band** |
  | 85 | 582,000 | 840 | 693s | **inside the band** |

  So the late bosses are already right and **only 20 and 40 need lifting** — the opposite of what the
  entry asked you. What is left to build is three separate things, and only the first is arithmetic:
  1. **Raise the low end.** The flat ×100 HP rank has to become a curve that does not collapse below 60.
     Measure it in `BalanceMatrix`, do not derive it.
  2. 🔴 **Defence and attack, not only HP** — *"stronger defences, more atk (not one shooting but a tank
     can feel it)"*. Today `Boss` is **HP ×100 / ATK ×10** with **no defence term at all**
     (`GameLoopService.cs:14014`), which is exactly why a boss reads as a sponge. A tank must feel the
     blows and must not be one-shot; that is a damage BAND, not a multiplier.
  3. 🔴 **Party composition must be mandatory** — *"A healer, tank and dds in a party are a must"*. A
     3-DD ceiling with no healer is what the current table measures, so the target itself needs
     re-basing on a real party before the numbers above mean anything.
  - ⚠ **Your `85j` EXP park now resolves itself**, in part: boss EXP is derived from kill time, so a boss
    that takes 3-10× longer carries its own increase without a separate ruling.
  - 🔵 **The world boss still has nowhere to live.** `MobRank` is Normal/Elite/Boss; only the respawn
    timer separates your 21-hour spawn from a 30-minute one. ~50 DDs for an hour is **~167×** a field
    boss — a new rank with its own drops/phases/lockout, not a bigger number. Not invented.

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

- `BL-81` 🔴 **DEBUFF IMMUNITY — GOD MODE IS ABSOLUTE, A BOSS IS NOT.** Playtest 25, and it is two rules
  in one message.
  - **Admin in god mode resists EVERYTHING** — *"cannot be debuffed (immune to all - can be used on him
    but resisted)"*. 🔑 **Read that clause carefully: the debuff must LAND AND BE RESISTED, not be
    refused.** A cast that is rejected outright tells you nothing about whether the skill works; a cast
    that resolves and reports a resist is a usable test of the skill you are debugging, which is the whole
    point of god mode.
  - **A boss is immune to CONTROL only** — *"not hold/slow/stun/paralize etc, but bosses can only have
    their p/mDef, p/mAtk lowered, can be Dot-ed and hp/mp regen limited"*. So the split is **control =
    immune** (hold, slow, stun, paralyze, and anything else that removes a boss's turn) against
    **attrition = allowed** (stat-down on the four defence/attack stats, damage over time, regen
    suppression). That is a per-effect classification, not a flag on the skill.
  - 🟡 **It lands on `BL-13`.** A boss that must last 10-30 minutes and be felt by a tank is a boss that
    cannot be perma-held, so this rule and that curve are the same design and should ship together.

- `BL-83` 🔴 **TAUNT MUST NEVER BE AUTOMATABLE — a reversal of the 0.68.0 fix, and he is right.** Playtest
  25, `85c`: *"I think remove the taunt as being able to be auto. I feel it like an exploit. Get a tank
  leave it auto he taunts almost impossible to kill you farm with ur other hero. Taunt should be active
  play only."*
  ⚠ **This undoes work from three days earlier.** 0.68.0 discovered that no taunt had ever fired from the
  auto chain (a taunt is neither a contested effect nor a debuff school, so it sorted into the never-cast
  bucket) and gave taunts their own rung above Attack. The bug diagnosis was correct; the feature it
  restored is one he does not want.
  🔑 **Build it as a REMOVAL, not a revert.** The same change shipped a second thing that is good and must
  survive: **an armed row the chain cannot cast is now reported the moment you save**, instead of being
  skipped in silence — which was the answer to his *"check the cyclic logic ...I feel there is a
  problem"*. A taunt should now appear in that report as *deliberately manual*, so an armed Provoke tells
  him why it never fires rather than looking like the same bug again.
  🔵 Open: does this cover **every** threat skill (`Lure`, the mob-only rogue pull, and any 40+ taunt from
  `BL-02`) or only single-target taunts? Assume all threat-generating skills unless he says otherwise.

- `BL-84` ⏸ **RENAME EVERY SKILL ID TO MATCH ITS NAME — QUEUED BEHIND THE HEALER, ON HIS INSTRUCTION.**
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

  ⚠ **It lands on top of an unruled boss curve.** `BL-13` says a flat ×100 swings boss difficulty
  **11×** between level 20 and 76, and `BL-49` says one boss kill is worth **1000× more** at 20 than at
  85. A 50% gem drop hangs a real reward on that curve, so a level-20 boss becomes the cheapest gem in
  the game by a wide margin. Build the gems whenever you like — but the drop chances are not meaningful
  until those two are ruled, which is another reason the % are explicitly yours to move.

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

---

## World & mobs

- `BL-47` 🔵 **`G3` — mobs built like players. ALL THREE STEPS ARE DONE, AND YOUR VERDICT SPLITS IT IN
  TWO.** You fought them in playtest 25 and marked all four rows `[x]`, then wrote the real answer beside
  them: ***"It works"*** — and four objections that stop it becoming the general mob pipeline.
  - ✅ **THE DECISIVE COMPARISON IS ANSWERED: *"they relatevley feel the same"*.** That is `88b`, the
    held **War Rune** against the authored **×2.07 attack passive**, and it means **the attack side of a
    player-built creature can be an item it carries** — no per-band attack table, no drift with level,
    and a creature that visibly holds the thing that makes it dangerous. This survives everything below;
    whatever pMobs end up being used for, they get their damage from a rune.
  - 🔴 **YOUR OBJECTION, and it is the one that matters: pMobs LOSE THE GLOBAL LEVER.** *"I have the
    feeling that controling the curve and the mobs per lvl will be harder this way ... with current mobs
    we can say: 'this one will have x2 hp' and whole the mobs on the field are altered.. while with the
    pMobs we will alter one and it will be good in the lvl range (+-5) not across the board."* This is
    correct and it is structural: `MobBaseStats` is one function, so one edit moves every creature in the
    game, while a per-creature loadout has to be re-authored one at a time. **A wholesale migration is
    therefore off** unless a curve-wide knob comes with it.
  - 🔴 *"I open now the skills tab and see a passive with much random numbers"* — the authored passives
    read as noise on the sheet. If passives survive at all they need to be named and rounded, not
    per-band decimals.
  - 🆕 **YOU NAMED WHERE THEY SHOULD GO INSTEAD, and both are better fits than the general roster**:
    **town/field guards** (`BL-79`) and **fortress sieges** (`BL-80`). Both are hand-placed, small in
    number, and want exactly what this pipeline gives — real gear, a real class, a visible loadout — with
    none of the curve-control problem, because nobody tunes fifteen guards with one number.
  - 🔵 **SO THE OPEN QUESTION IS NARROWER THAN IT WAS.** Not *"do we migrate"* — you already answered
    that in playtest 24 and playtest 25 walked it back to *"where"*. It is: **do ordinary field creatures
    stay on `MobBaseStats` with the ×2 passives, and pMobs become a hand-placed content tool?** That is
    the shape everything you wrote points at, and it costs nothing already built — the demo, the fence,
    the loadout inspector and the rune all serve it unchanged. **Say yes and `BL-79`/`BL-80` become the
    roadmap; say no and the global-lever problem needs solving first.**
  - ⚠ **Independent of this, mobs are too easy and too thin — that is `BL-78`**, and it is the same
    verdict whichever pipeline wins.

  **The record of steps 1 and 2 follows.** Your three ordered steps were *"I want it documented and
  balance matrix tables … and later we can do 2~5 mobs so I can test."* Both are done — the document is
  `design/MobsAsPlayers.md`, and **step 2 shipped 2026-08-16 (0.70.0)** as the **Proving Grounds**, a
  gatekeeper destination south of the training dummies holding five creatures built through the player
  pipeline, each beside the ordinary creature of its own level.
  - 🔑 **YOUR ±5 BAND WORKS, EXCEPT ON ATTACK.** The same authored loadout five levels apart holds
    defence and HP (P.Def x1.04 → x0.95, HP x1.10 → x1.06) and **loses a quarter of its P.Atk**
    (x0.87 → x0.64), because the mob attack curve is the steep one. So *"prefixed 100+ mobs and give
    them +-5 lvl ranges"* costs **one number per band, and it is the attack number**. Both goblins are
    left deliberately bare so you can feel the drift rather than read about it.
  - 🔑 **A HELD WAR RUNE REPLACES AN AUTHORED ATTACK PASSIVE — measured, your B3.** Bare, the level-80
    build reads **x0.48** of its curve's P.Atk. An authored per-band passive gets it to x1.00; **the
    rune gets it to x0.97**. One item against a table that drifts with level. If you like how it
    fights, the whole attack side of this design collapses into something a creature carries.
  - ⚠ **One number came out past your ×2 and it is nobody's mistake.** `G3.7` said the level-80 attack
    passive needed ×1.55; the creature needs **×2.07**. `G3.7` measured against the bare `MobBaseStats`
    curve — but what actually spawns beside it also carries **BL-14's weapon power factor**. `G3.8` is
    the section that measures against the game, and it is the one to trust.
  - **Everything below is step 1's record**, kept because it is what the rulings were made on.

  The document is
  **[design/MobsAsPlayers.md](design/MobsAsPlayers.md)**; the `BalanceMatrix` `G3` tables it reads from
  have existed since 2026-08-05. No game code was touched. Three things it found that change the entry:
  - 🔑 **The inflated ATK/CON you objected to are already inert.** `MobStats` still sets them (level 80 →
    CON 175 / ATK 168) but `RecomputeDerived` sends a mob to `MobBaseStats` for HP, MP, P.Atk, M.Atk,
    P.Def and M.Def — **not one of them reads either stat**. Only AGI 30 and WIT 5 do anything. What you
    actually saw is a DISPLAY: the target sheet printed both numbers. ✅ **Fixed 2026-08-16** — a mob's
    Attributes block is now AGI and WIT only (SPT went too; `MobStats` says in its own comment that mobs
    never read it). No simulation change, and it answers *"it looks over inflated"* on its own.
  - 🔑 **Four of your five passive families already ship** as `MobMasteries`/`MobMod` + 0.65.0's mob
    weapon types. What is genuinely missing is small: armor weight has **3 rungs and no robe arm** (you
    asked for ~15 and a caster rung), the weapon type carries ~4 of your 7 axes, and **speed has no
    track at all**.
  - 🔴 **§8-B IS ANSWERED — playtest 24 (2026-08-16): MIGRATE.** The doc recommended finishing the
    passive layer instead; he rejected the premise it rested on. His words: *"u said u cannot manage to
    balance a player with current mobs curve ... human fighter with S grade Mace enchanted to +60
    (that's why we van have a mobs weapons) and B grade leather only have the same pDef and twice less
    p atk g if we make the elite passive x2 p atk and hp boost we can make him the same values ... try
    to recreate mobs with different races (main stats) with player formulas ... so same weapon type and
    just enchanted or a mob passives that boost PAtk and or other stats"* — plus, in chat the same day:
    *"If we have different mob races like litches,angels,goblins etc all will have different main stats
    (near players one) and just boost with passives and lower gears."*
    The superseded recommendation is in [BacklogArchive.md](BacklogArchive.md#bl-47).
  - 🔑 **He named the levers `G3.2` never swept, the sweep was re-run his way as `G3.7`, and HE IS
    RIGHT.** Two blind spots: the enchant axis stopped at **+16** (a player's practical ceiling — a mob's
    enchant is just an authored number), and every slot moved **together**, so an over-enchanted weapon
    over under-grade armour was never constructed. Swept separately, weapon enchant to +60:
    **12 of 16 archetype-levels land inside his ×2 passive on all four stats at once**, the worst single
    miss drops **185-221% → 94%**, and the biggest attack passive still needed anywhere is **×1.60**.
    The optimiser picked his loadout unprompted — lowest-tier armour, weapon at level tier plus enchant.
    ⚠ **The four failures are one failure: the Nuker's HP** (×2.01 at 20 → ×3.48 at 80). Every P.Def,
    M.Def and attack figure is inside ×2 at every level. His *"and hp boost"* already allowed for it.
    🔵 Next lever if the 80 row needs tightening: `G3.7` still dresses all nine slots, and at 80 the
    binding constraint is **M.Def over-delivering ×1.65** — a creature need not wear jewels at all.
  - 🔑 **RACE as the main-stat carrier is the better shape and it costs nothing to adopt.** The doc used
    player *archetypes*, which §5 correctly called invented machinery; races are content the world wants
    anyway, so the "mob archetype table" stops being scaffolding and becomes the thing being built.
  - ✅ **He answered the three questions that gated step 2, same day (2026-08-16).**
    **Race = a flat ±5 stat offset, no level curve** (*"ork have higher con/atk less agi ..while elf have
    higher agi less atk/con ... Can go +-5 same as the swap passives"*) — ⚠ which makes race **flavour,
    not the reconciliation**: ±5 on a ~40-point stat is ±12.5% against passive needs of ×1.5-2.0, so a
    lich differs from a goblin by **kit, gear and passives**. **A demo first, the roster number after.**
    **A mob may hold an inventory** — *"not a dropped one..but just to hold stuff"* — so **yes to the War
    Rune**, held and never looted. And **balance against NORMAL mobs**, elite/boss scaling on top: ✅ every
    `G3` number already does, since rank multipliers are applied at spawn. 🔑 For the record, since he
    could not remember: **Elite = HP ×4 / ATK ×1.5, Boss = HP ×100 / ATK ×10** (`GameLoopService.cs:14014`).
  - 🔴 **HIS ROSTER RULING, and it is ~90% already built.** *"we can do a IG logic... Prefixed 100+ mobs
    and give them +-5 lvl ranges so they can offset a bit ... Not a lvl 1 mob scaled with lvl to 85."*
    **`MobCatalog` already holds 80 templates, each with its own natural level, ~2 levels apart**, and
    `GameLoopService.cs:13959` gives a natural level priority over the zone band. What is missing is the
    **±5 variance** (today ±0) and ~20 more templates. 🔑 **This retires `G3.3` as an objection**: the
    "frozen loadout rots to 6% of curve at 85" test stretched one template across 65 levels, which this
    catalogue never does — so a **level→grade function is not mandatory** after all. ⚠ The one place that
    does stretch a roster is **`zone.ForceZoneLevel`** (the 85-90 field), which is the thing he objects to.
  - ✅ **Step 2 is BUILT** (see the top of this entry) — both decisive comparisons came back answered.
    §8 **C/D/E remain open** (armor-weight rungs and the robe arm · whether the weapon type carries
    `matk`/`cast`/`critdmg` · speed as a passive), and none of them blocked the demo.
  - ✅ **"Then we do a system number" — playtest 25 answered one of the two.** The rune wins over the
    passive (they feel the same, and the rune does not drift). ⚠ **The ±5 attack drift was never
    commented on** — you marked `88a` `[x]` and wrote about the whole demo instead, so whether an Elder
    Raider feels too soft for its level is **still unanswered**, and it only matters if pMobs end up
    carrying a band at all. The roster count itself is moot under the narrowed question above: a
    hand-placed tool has no roster number.

- `BL-48` ⏸ **Instances — you are holding.** Design is written (`design/Instances.md`). One
  load-bearing decision is still open: the daily attempt **GLOBAL vs PER-INSTANCE**. It changes the
  persisted model, so it is answered before anything is built. **Dungeons are the cheap half** —
  a dungeon is just a `SpawnZone` outside the town ring plus a teleport entrance, near-zero risk,
  and they can ship without instances.

- `BL-49` 🔵 **Levelling pace — the boss half is BUILT (0.67.0), two items are left and both need
  your eye.** The elite/boss EXP multiplier is done and closed: your *"x1.2~2"* is now 1.2 elite /
  1.5 boss over a measured kill-time ratio, and it fixed a silent 5× underpayment (the old rule was
  HP-only and clamped at 20× while a boss carries 100× HP). What is still open:
  - **The absolute value of a boss kill swings 1000× across the game.** One level-**20** field boss
    is **125% of a level** solo; a level-**85** one is **0.1%**. Both are the same 150 trash kills,
    so this is the LEVELLING CURVE, not the boss rule — but a low-level boss handing out a level and
    a quarter per kill is a decision, not an accident, and it is yours. `tools/BalanceMatrix`, the
    `BL-49` table.
  - **The 60-85 band and the fighter kill-speed sanity check**, neither ever run. Note the cumulative
    trash-kill count reaches **631k by level 86** against 21k by level 62 — whatever the old *"60-85
    runs ~3× faster"* note meant, the measured curve now says the opposite and wants your call.

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
  the name-root families, a 450 radius, damage-only trigger, and a no-damage mob-only taunt at
  20/28/36 whose ladder is reach (200/400/600). See `CHANGELOG.md`. Delete at the next sweep.
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

- `BL-79` 🔴 **TOWN / FIELD GUARDS — the first real use for `BL-47`'s player-built creatures, and it is
  small.** Your design, playtest 25, verbatim: a **Lv 80 mob in Mythic t80** that is *"only aggressive
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

- `BL-82` 🔴 **YOU CANNOT SEE THAT YOU ARE IN GOD MODE OR INVISIBLE.** Playtest 25: *"Add a flag for admin
  to see that he is in god/invis ... but now i cannot see nothing."* Two halves, and the first is small
  enough to ship on its own:
  - **Now:** a persistent on-screen indicator for each staff state that is currently silent — god mode and
    invisibility. Text or a badge; the client already knows both, nothing new has to be sent.
  - **Later, once models exist:** your rule for the whole stealth family — *"the players in shtealt will
    see themselves with opacity to 0.7 and in invis 0.4 (for them selves only - for others stealth does
    nothing, invis vanishes them)"*, and **a god admin gets a golden colour or border**. 🔑 The important
    half of that sentence is *for themselves only*: **an observer must learn nothing from it**, which is
    the same rule `BL-69` already enforces server-side (hide is an OMISSION from the snapshot, not a flag
    the client is trusted to honour). So the opacity is a purely local effect and must never be derived
    from anything sent about another player.

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
