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

**G4. ✅ BUILT 2026-08-05. Save-login checkbox on the client.**
> Add a checkbox to the client to save login information or not - now always the password field is
> "admin"

> **Built:** `[x] Save login on this device` under the password field (a full-width button, not a
> 20px box — the kit has no toggle and a phone wants a tap target; `[x]`/`[ ]` are ASCII because the
> TMP atlas has no tick glyph). ON stores username **and** password after a login that actually
> SUCCEEDED — storing on submit would remember a typo forever. OFF stores neither, comes up blank,
> and **wipes what is already on disk** rather than merely stopping future writes. The server ADDRESS
> is remembered either way: it is not a credential. Default ON, and with nothing saved yet the
> inspector's admin/admin still fills in, so the debug rig is untouched — the moment he logs in as
> anyone else, that is what comes back, which is the whole of the complaint.
> ⚠ PlayerPrefs is not a secret store (a plain XML file in the app's private data). That is what
> every phone "remember me" gives, and it is why this is a choice rather than unconditional.

**G5. ✅ BUILT 2026-08-05. The Dash potion and the rogue's Sprint must not overlap — full spec, his:**
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

> **Built:** Sprint joined the `dash` family, and the family is now ranked by MAGNITUDE end to end:
>
> | rank | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
> |---|---|---|---|---|---|---|---|---|
> | | Dash C +15 | Dash U +30 | **Sprint L1 +40** | Dash R +45 | Dash E +50 | Dash L +55 | Dash M +60 | **Sprint L2 +60** |
>
> That is his two sentences exactly: Sprint L1 replaces Dash C/U (and is refused under anything
> stronger), Sprint L2 replaces everything including Sprint L1. **Sprint L2 sits above Dash M at the
> same +60 on purpose** — a class skill you levelled must not be overridable by a bottle, the same
> rule a group buff follows. Durations 15s both; reuse Sprint 30s / potion 60s, both already correct.
>
> ⚠ Sprint's two levels need DIFFERENT ranks and `Rank` lives on the `SkillDef`, not on `SkillLevel`
> — so Sprint is authored as a **one-child wrapper** whose level picks the child (`buff_sprint_1` /
> `buff_sprint_2`), the same machinery a potion uses. Its old private `"sprint"` BuffKey is gone;
> that key having a family to itself is precisely what let you hold Sprint and a Dash potion at once.
>
> **Two things I decided, both one line to reverse:**
> 1. **The potion keeps all SIX rungs.** He named four (C15 U30 R45 M60); Epic +50 and Legendary +55
>    also exist, are in the drop tables, and deleting them is a content change, not a stacking fix.
>    Ranks 5 and 6 are theirs. Say the word if he wants the ladder cut to four.
> 2. **Sprint level 2 is learned at 40.** He gave the level and the value but not where — and the
>    authored rogue CSV stops at 36. 40 is the next rung on that block's own 4-level cadence and
>    where the 3rd-class disciplines already sit. Without a grant, level 2 is unreachable.

**G6. ✅ BUILT 2026-08-05. The warehouse must show slots used / total.**
> Top-right of the window, per bank (private vs account have different caps), and it turns **red** once
> there is no room left — until now a full warehouse announced itself only as a refusal in the chat log.
> in the warehouse need to see spots taken/all - now i try to deposit and cant .. only after opening chat
> i saw that warehouse is full

**G7. ✅ BUILT 2026-08-05. A hotbar consumable slot at 0 count must not be DISABLED.**
> Disableing hotbar potion/consumable slot when I have 0 - means i cannot remove it from the bar.. make
> it like always in 100% cooldown - it looks the same just is not disabled.

The slot going dead also kills the drag/long-press that would remove it — the bar traps a slot you can
never clear. Draw it as a full cooldown sweep instead: same look, still interactive.

✅ Exactly that. `PressAndHold.Enabled` is wired to the button's `interactable`, so an out-of-stock item
slot lost the one gesture that opens Move/Remove/Auto. Such a slot now stays interactable and draws the
reuse sheet at full height with no countdown text (there is no timer, only an empty bag), and `FireSlot`
returns early so the tap is inert rather than earning a refusal from the server.

**G8. The rogue's Weapon Mastery has its crit damage swapped between 24 and 28** — *"i fixed it in the
rogue-csv"*. Already the second bullet of RoadmapNext 🔴 3.

**G9. `crit dmg` in the CSVs is FLAT, not a multiplier** — *"x0.8 .. its + .. so its flat increase. The
formula should have it -> added to base atack before the critical dmg % increase."* Already ruled
2026-08-05 and specified in [design/CritBlowAndDouble.md](../design/CritBlowAndDouble.md).

---

## Quest

**Q1. ✅ BUILT 2026-08-05. 🔴 Quest tracking is not persistent.**
> Quest tracking is not persistant. - I restarted the server and dont know if is because of logout or
> just not peristant per character

**Diagnosed — it is neither logout nor the server.** The tracker is a client-side
`List<string> _trackedQuests` in `GameUi.Quests.cs` and nothing ever writes it anywhere: not to the
server, not even to PlayerPrefs. So it dies with the app, and it is per-INSTALL rather than per
character. It must be stored per character; server-side alongside the quest log is the honest place.

> **Built:** the pin is a `Tracked` flag on `CharacterQuestState` — the per-character quest progress
> that is already persisted (`ActiveQuestsJson`), so it survives a relog and follows the character to
> another phone. **No schema change and no db reset:** an old save simply reads `Tracked = false`.
> The client no longer owns the list at all; it reads `QuestEntry.Tracked` and asks the server to
> toggle, which is the same rule the skill bar taught — the client never authors state it did not
> receive. The cap (5) moved to `GameConstants.MaxTrackedQuests` so both halves agree on it.

**Q2. ✅ BUILT 2026-08-05. Accepting a quest auto-tracks it.**
> Pinned by the SERVER on accept, so it is true however you took the quest. It **yields** at the cap
> rather than evicting: an automatic pin has no business pushing off one you chose. (An explicit
> Track past the cap still evicts — you asked for that one.)

**Q3. ✅ BUILT 2026-08-05. The tracker row shows only the objectives** (items / kills), not the full
description.
> It reads the structured steps now instead of the dialog's pre-formatted step sentence: a gathering
> contract lists `item  held`, a kill step puts its `3 / 10` on the same line as the objective, and
> the description, the location and the story are gone.

**Q4. ✅ BUILT 2026-08-05. Clicking a tracker row opens that quest's DETAIL page**, not the quest
window's list.
> The tracker was one block of text with nothing to click; it is now one tappable row per pinned
> quest, each opening its own Details. Dragging the panel still works — a drag cancels the tap.

**Q5. ✅ BUILT 2026-08-05. The Active tab's rows must be short, like the Available rows** — name plus
"Ready to hand in", level range, the give/return NPC, steps. Not the full text.
> Quest window in the Accepted tab the row must be only the Name and some short info -- like the
> Available rows --> "Ready To hand in", lvl range, return/taken npc, steps -- not full details

> **Built:** an Active row is now name + step `2 / 3` + level band, the status line, the progress
> NUMBER (and the gathered count for a contract), and **From: <npc> — <town>**, which every tab now
> carries. The step text, "Where:" and the mob name are gone to Details.

(Q3 and Q5 are the same rule as C6: full text lives in Details and nowhere else.)

---

## Farming

**F1. ✅ BUILT 2026-08-05. Turning auto-farm OFF must not drop what you are fighting.**
> When disablaling auto-farm not to cancel target and close target window and stp attacking - i must
> reselect mid fight to finish the kill

Switching to manual should leave the target, the target window and the attack running — only the
autopilot stops.

> **Diagnosed:** the server was still swinging the whole time. `AutoPilot` pushed `AutoTarget(null)`
> on the first tick after the toggle — "the window must not keep showing the autopilot's last pick" —
> and the client's `TargetId` follows that push, so the selection vanished while `CombatTargetId`,
> `Engaged` and `AttackCommandTargetId` were all still set. Re-selecting "to finish the kill" was
> re-selecting something already being hit.
>
> **Built:** switching to manual now **hands the target over** instead of cancelling it — the push
> re-sends the current target as long as it still names something alive and present, and only then
> null. Nothing else is touched, so the swing continues and only the autopilot stops. The window
> still clears the moment the target is genuinely gone, and the paths that really end a fight
> (`StopAutoHunt` on death or a spent budget) clear `CombatTargetId` themselves, so they push null
> exactly as before.

---

## Vendors

**V1. ✅ BUILT 2026-08-05. A quick-sell toggle, mirroring the bin: `[QSell On/Off]`.** With it on,
`[Sell]` sells the max amount in one tap instead of asking for a quantity — the same shape as the
inventory's `[Del On/Off]`.

> **Built:** `QSell: off / ON` beside the Sell tab, in the bin's armed red when on, and **hidden on
> the buy side** — a toggle that goes dead when you tap Buy reads as a bug (the same reasoning that
> put the category filter on both lists rather than blanking it). With it on a row sells the WHOLE
> stack on one tap: no numpad, no confirm. It deliberately covers the non-stacking case too — a
> single sword is one tap either way, and a "quick" sell that still popped a dialog for half the
> rows would not be one. The window title says so while it is armed. Unlike the bin it may skip the
> confirmation outright, because a sale is undoable from the buy-back list.

**V2. 🔴 Sell price = 0.25 of the buy price, for EVERYTHING.**
> All items must be sold for .25 of their price .. now they are sold for .8 (equipment)
> I know this will lower the price of A/S grade but the idea is not selling in the shop getting rich
> from trash.

✅ **RESOLVED AND BUILT 2026-08-05 — but not as written.** The 0.8 was his own misread: he sold a
**Feretite Robe** and read the number as the gloves' price. So there was no bug, and the ask became the
real one underneath it — *"selling items/trash making money ok .. but not farming"*.

He then produced the best economy datum this project has: **three characters, same ~14-15 h idle farm.**
Mage 34 selling nothing = **350k**. Tank 36 selling only equipment = **3.3kk**. Rogue 34 selling
everything = **4.6kk**. BalanceMatrix reproduces all three and shows **gear is the entire faucet** (10×
the coin drop; mats + potions are 2%).

**Shipped:** the cut went on the **drop rate**, not the price — cutting the price alone leaves you
wading through the same flood of junk, which is the actual complaint. Gear groups **×1/3 → ×0.025**
(13× rarer) and `GearSellDivisor` **25 → 10** (each drop worth 2.5× more). Measured: **4,055,588 →
1,227,289** over that farm, gear:coin from 10.3× down to 1.9×.

**V2b — the scroll pass, same day, also BUILT.** He corrected the consumable finding: Return/Resurrection
were already cut 20×/200× and *"are usefull u wont be seling all"*; and *"if you sell them not trade or
ecnaht the gear .. well goodluck not being part of the economy."* His ask: enchant + attribute scrolls
need *"lower the chances + move them in the lvls a bit"*. Measuring it flipped the diagnosis twice:

- **Enchant and attribute scrolls already sell for 0** (no `Value:` on the ItemDef) — they cannot feed
  the gold economy at all. The reason to cut them is the BAG, not the faucet.
- **The consumable gold was buff potions/scrolls**, 155/kill. His playtest-17 *"buff pots are 0 sell"*
  had **never been implemented** — and the ÷10 had just made them 2.5× richer.
- **Attribute scrolls at 27%/kill were an accident**: independent rolls take the global ×3 that the
  guaranteed groups are exempt from. Authored 0.09, delivered 0.27.

Shipped: `SellPriceOverride: 0` on every buff potion / buff scroll / Dash potion · enchant share of a
scroll rung 0.5 → **0.15**, floors 1/20/45 → **10/30/55** · attribute chances cut ~5× and spread over
their band (floors 40/52/61/76/80/84). Measured at level 33: enchant 30% → **9%**, attribute 27% →
**3.6%**, buff gold 155 → **2**/kill. **Total 1,038,115 = 1.04× target** (gear 65 / coin 34 / cons 1).

⏭ **Still open, not urgent:** gear value follows the tier ladder while coin is linear, so the ratio
still drifts to 51× by level 76 — the fix there is the **coin curve**, not another multiplier.
Full detail: `docs/design/EconomyRework.md` §4a + §4b.

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
5. **G5** — two calls I made to ship it: the Dash ladder KEPT all six rungs (he named four — Epic +50
   and Legendary +55 also exist and are in the drop tables), and **Sprint level 2 is learned at 40**
   (he gave the value, not the level; the rogue CSV stops at 36). Both are one line to change.

---

## Where the queue stands (2026-08-05)

**Built:** G6 G7 · Q1 Q2 Q3 Q4 Q5 · V2 V2b · **F1 · V1 · G4 · G5**.
**Not built:** **G1** (the skill deletion — unblocked by his answer, but still needs the two rulings
above), G2 (answered, awaiting his keep/delete), G3 (design, needs his go), G8 + G9 (already on
RoadmapNext 🔴 3 / `CritBlowAndDouble.md`).
