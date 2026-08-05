# Playtest-18 — the second 0.45.0 pass (owner, 2026-08-04)

**Source: his own file, `mytest-26216`.** As with [Playtest-17.md](Playtest-17.md) this is the
AUTHORITATIVE queue: his wording is kept verbatim under each item, my reading is the line above it.
Ids are namespaced by his own headings — **G**eneral / **Q**uest / **F**arming / **V**endors.

**The verdict in one line:** not a bug hunt — this is the **answer to the B3 deliverable** (which skills
to delete), two design questions back at me, and a list of interface friction he hit while playing.
Only three items are defects: quest tracking doesn't survive a restart, the vendor sell fraction is not
the number he expects, and the hotbar disables a consumable slot you can then no longer remove.

> One line in his file — *"[!] Just noticed the clerics(@20) all the passives"* — he told me to
> **ignore**; it was left over from the test. Not an item.

---

## General

**G1. 🔴 The skills to delete — this is his answer to B3, and it unblocks the deletion.**
> - `evade_mastery`, `reflexes`, `precision`, `anti_magic` — the four "identity floor" passives.
> - `archer_armor_mastery`, `archer_weapon_mastery` — orphaned by the archer→rogue merge.
> - `dispel_magic`.
> - God class + skills

That is §3 of [Skills-Not-In-CSVs.md](Skills-Not-In-CSVs.md) — the "granted to NOBODY" list — minus
two entries he did **not** name: `class_balance_*` (8) and the commented-out `lb_*` / `wc_*`, which he
asks about instead (G2). **God class + skills** = `hp_boost`, `greater_heal` **and the god table
itself** (`ItemRarity.God` and the never-registered class stay or go with it — ask before widening).
⚠ The original B3 ask also covered **Heavy Draw**: the safe operation there is deleting the **Rogue @24
grant** of `power_shot`, never the definition — three level-40 discipline skills are renames of it.
Twin Slash is already gone below 40.

**G2. ❓ What are the commented-out Lightbringer (8, `lb_*`) and Warchanter per-race (12, `wc_*`)
skills?** ✅ **ANSWERED — written up for him at the end of §3 of
[Skills-Not-In-CSVs.md](Skills-Not-In-CSVs.md).** They are the **level-40 HEALER disciplines**:
Lightbringer = the pure healer (per-race heal/cleanse/root/anti-heal + a shared party buff and passive),
Warchanter = the buffer (per-race nuke / mega chant / party HoT / passive). The defs are registered and
alive; what is commented out is **one line of learn assignments** in `ClassSkillTables.Third.cs`, dropped
pending his level-40 CSVs. ⚠ The Warchanter's **buff** layer is separate and IS live
(`RegisterWarchanterBuffs()` — every group buff and Harmony in play today). **My recommendation: keep
both.** They are unreachable, so they cost nothing, and they are most of a 3rd-class healer kit already
written — one uncommented line when his CSVs land. His call.

**G3. ❓ Mobs built like players — no inflated STR/CON, real gear instead.**
> Can monsters have no inflated stats like the con and the str and have items (like weapons armors and
> jewels) - they will start to use normal formulas and be like weaker players - they will have items like
> claws, talons armor like feather, skin, skeletons can use real weapons like blades and aegises and
> armors like bulwark/leather

A real design change, not a tweak: it would move mobs off `MobBaseStats` onto the player pipeline
(`RecomputeDerived` + equipment), so a mob's difficulty would come from its kit. Attractive — it makes
mob stats readable and reuses one formula set — but it touches every mob, the level-assigns-stats rule
and probably the drop tables. **Needs a design pass and his go before anything moves.**

**G4. Save-login checkbox on the client.**
> Add a checkbox to the client to save login information or not - now always the password field is
> "admin"

**G5. The Dash potion and the rogue's Sprint must not overlap — full spec, his:**
> Dash potion is the same as sprint skill just weaker (longer cd and weaker value)
> - Both are 15s, Potion CD 1min, Sprint cd 30s
> - Potion - C15, U30, R45, M60 -> Effect: E1, E2, E4, E5
> - Sprint L1-40, L2-60 -> Effect: E3, E6
> - Same effects or weaker are removed and replaced by the new effect -> SprintL1 replaces Dash C/U,
>   SprintL2 replaces Dash C/U/R/M and SprintL1 …

Read: one **speed family** with six rungs, ordered `E1 < E2 < E3 < E4 < E5 < E6`, where the potion owns
E1/E2/E4/E5 (Common 15 / Uncommon 30 / Rare 45 / Mythic 60) and Sprint owns E3 (L1, +40) and E6 (L2,
+60). Both last 15s; potion cooldown 60s, Sprint 30s. This is exactly the existing **family + Rank**
machinery (`ApplyBuff`) — a stronger rung replaces a weaker one and a weaker one is refused — so it is
an authoring change, not new code.

**G6. The warehouse must show slots used / total.**
> in the warehouse need to see spots taken/all - now i try to deposit and cant .. only after opening chat
> i saw that warehouse is full

**G7. 🔴 A hotbar consumable slot at 0 count must not be DISABLED.**
> Disableing hotbar potion/consumable slot when I have 0 - means i cannot remove it from the bar.. make
> it like always in 100% cooldown - it looks the same just is not disabled.

The slot going dead also kills the drag/long-press that would remove it — the bar traps a slot you can
never clear. Draw it as a full cooldown sweep instead: same look, still interactive.

**G8. The rogue's Weapon Mastery has its crit damage swapped between 24 and 28** — *"i fixed it in the
rogue-csv"*. Already the second bullet of RoadmapNext 🔴 3.

**G9. `crit dmg` in the CSVs is FLAT, not a multiplier** — *"x0.8 .. its + .. so its flat increase. The
formula should have it -> added to base atack before the critical dmg % increase."* Already ruled
2026-08-05 and specified in [design/CritBlowAndDouble.md](../design/CritBlowAndDouble.md).

---

## Quest

**Q1. 🔴 Quest tracking is not persistent.**
> Quest tracking is not persistant. - I restarted the server and dont know if is because of logout or
> just not peristant per character

**Diagnosed — it is neither logout nor the server.** The tracker is a client-side
`List<string> _trackedQuests` in `GameUi.Quests.cs` and nothing ever writes it anywhere: not to the
server, not even to PlayerPrefs. So it dies with the app, and it is per-INSTALL rather than per
character. It must be stored per character; server-side alongside the quest log is the honest place.

**Q2. Accepting a quest auto-tracks it.**

**Q3. The tracker row shows only the objectives** (items / kills), not the full description.

**Q4. Clicking a tracker row opens that quest's DETAIL page**, not the quest window's list.

**Q5. The Active tab's rows must be short, like the Available rows** — name plus "Ready to hand in",
level range, the give/return NPC, steps. Not the full text.
> Quest window in the Accepted tab the row must be only the Name and some short info -- like the
> Available rows --> "Ready To hand in", lvl range, return/taken npc, steps -- not full details

(Q3 and Q5 are the same rule as C6: full text lives in Details and nowhere else.)

---

## Farming

**F1. Turning auto-farm OFF must not drop what you are fighting.**
> When disablaling auto-farm not to cancel target and close target window and stp attacking - i must
> reselect mid fight to finish the kill

Switching to manual should leave the target, the target window and the attack running — only the
autopilot stops.

---

## Vendors

**V1. A quick-sell toggle, mirroring the bin: `[QSell On/Off]`.** With it on, `[Sell]` sells the max
amount in one tap instead of asking for a quantity — the same shape as the inventory's `[Del On/Off]`.

**V2. 🔴 Sell price = 0.25 of the buy price, for EVERYTHING.**
> All items must be sold for .25 of their price .. now they are sold for .8 (equipment)
> I know this will lower the price of A/S grade but the idea is not selling in the shop getting rich
> from trash.

⚠ **His measurement does not match the code and must be reconciled before anything changes.** At HEAD:
tiered gear sells at **buy ÷ 25** (4 %, `GameConstants.GearSellDivisor`), use-consumables the same,
everything else at **× 0.30** (`VendorSellFraction`) — no path produces 0.8. Either he read a different
number than I think he did, or an item class is escaping the tiered path. ⏭ **Ask him which item he
measured on** — moving the constant to 0.25 blind would *raise* the gear faucet 6×, the opposite of what
he is asking for.

**Later, not now — trash becomes crafting mats instead of gold:**
> Later we can make trash disasemble for crafting mats (rarity for mats rarity) (grade for mats ammount)
> - common -> common mats (F common 1-2, E common 5-10 ... S common - 500-1000)
> - uncommon -> uncommon mats (F common 2-3, E common 5-10 and uncommon 1-2 ... S common - 500-1000 and
>   uncommon (100-200))
> - etc... -> higher grade lowers the previous grades mats and increasing own

Rarity picks WHICH mats, grade picks HOW MANY, and each rarity step trades some of the previous tier's
mats for its own. **This belongs to crafting** — file it with [design/Crafting.md](../design/Crafting.md).

---

## ⏭ Owed back to him (blocking his decisions)

1. ~~**G2** — what `lb_*` (8) and `wc_*` (12) are~~ — ✅ answered in Skills-Not-In-CSVs.md; awaiting his
   keep/delete ruling (my recommendation: keep).
2. **G1** — a ruling on `class_balance_*` (8), which he skipped, and confirmation that "God class +
   skills" means the table as well as `hp_boost` / `greater_heal`.
3. **V2** — which item he saw selling at 0.8.
4. **G3** — my design read on item-carrying mobs before he commits to it.
