# Test Checklist — Unity client (Android / Editor)

Companion to `TestChecklist.md` (which covers the WPF client + server behaviour). Same
conventions: **`[ ]` = not tested, `[x]` = verified, `[~]` = tested, needs a change/tuning.**

The Unity client is a **thin view over the same server** — it shares `Game.Shared` and speaks the
real protocol through `NetworkChannel`. So most items here test the CLIENT, not the game: if a
number is wrong in both clients it's a server bug and belongs in `TestChecklist.md`.

**Test in this order.** Every section depends on the one above it — a failure at step 2 makes
everything below it meaningless, so stop and report at the first ✗ rather than ticking on.

---
## ✅ ⚙ First-run setup — VERIFIED 2026-07-24

`activeInputHandler = 2 (Both)`, server on `:5238`, `adb reverse` (or LAN/VPN IP) and the adb path all
confirmed working. **Landmines, do not undo:** `Assets/link.xml` must stay (IL2CPP stripping breaks the
PHONE build only, so desktop testing never catches its return); there is ONE Android package, so every
APK installs over the last one; `builds/L2Clone.apk` is the only APK.
Headless builds work with Unity CLOSED — see `Assets/Editor/CommandLineBuild.cs`. The exact invocation
(the method name must be FULLY QUALIFIED — passing bare `CommandLineBuild.BuildAndroid` fails with
"executeMethod class could not be found", and Unity still exits 0 through a shell wrapper, so it looks
like it worked while quietly building nothing):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -quit -batchmode -nographics ^
  -projectPath "G:\Work\Repository\L2Clone\Game.Client.Unity" ^
  -executeMethod Game.Client.Editor.CommandLineBuild.BuildAndroid ^
  -logFile "<some>\unitybuild.log"
```

Check the log for `error CS` (compile) and for `executeMethod class ... could not be found` (wrong name)
— an APK whose timestamp did not change is the tell. Note this is also the ONLY way to compile-check the
client: `dotnet build Game.sln` does not include the Unity project, so client-only edits are unverified
until a headless build runs.

## 1. ✅ It starts and tells you something — VERIFIED ON DEVICE 2026-07-21

Status strip, Sign in panel, finger-sized scaling, soft keyboard, and the Log console all confirmed by
screenshot on the S23 Ultra.

## 2. ✅ Connection — honest failure first — VERIFIED 2026-07-24

Refused logins report the reason in red without hanging, the dot stays red/`Offline`, a later Login
connects with no app restart, Register/wrong-password behave, and the version handshake gates entry.

## 3. ✅ Character select — VERIFIED 2026-07-24

The account's characters list with `Name / Lv / Race / Class` matching WPF; create (Human/Elf/Ork, no
God) works; Enter reaches `InWorld`; Logout returns to Sign in, clears cached credentials (no silent
re-login) and a different account shows its own characters.

## 4. ✅ The world renders — VERIFIED 2026-07-24

The `SnapshotDelta` path is proven end to end: your own entity is present on the first frame, entities
render in their right colours (green self / red mobs / yellow NPCs / cyan players), nameplates + HP bars
draw, aggressive mobs end in `*`, level privacy holds, despawn/respawn is clean and corpses dim.
**Landmine:** the magenta-everything bug was `Shader.Find("Unlit/Color")` returning null in a URP
*player* build — `UnlitMaterials.cs` + Always Included Shaders fix it; don't remove them.
**Still worth watching:** if the log ever prints *"World state out of sync — asking the server to
re-send it"*, that is itself a finding — report it, don't shrug at a working screen.

## 5. Movement — VERIFIED, one change wanted

- [x] Tap-to-walk, `pos` updates, the sliding ground grid, smooth interpolation, cross-client
      confirmation from WPF, Walk/Run speed toggle, and Sit/Stand blocking movement all work.
- [ ] 🔧 **Stand-up timing (playtest-11)** — there should be a **delay after tapping to stand**, but
      **NOT** if you have been sitting **longer than 3s**, in which case standing is **instant**.

## 6. ✅ Combat + HUD — VERIFIED 2026-07-24

Targeting, the Attack button, HP drain on both panel and nameplate, self-only combat log lines,
HP/MP regen in 3s chunks, the XP bar + `Level up!`, gold matching WPF, Die→Respawn, and a clean Leave
back to character select all confirmed.

## 7. ✅ Input hygiene — VERIFIED 2026-07-24

Tapping UI never leaks a walk, the console scroll area scrolls instead of walking, and after Send the
keyboard closes and the next ground tap walks. (One exception found — see §9 and the field banner in §17.)

## 9. Command bar, chat and slash commands — MOSTLY VERIFIED, one bug + additions

- [x] Command bar visible in world with its hint; local chat reaches a nearby WPF client; `!message`
      world chat; `/w Name message` whispers privately; `/w Name` alone prints usage; `/fadd` `/flist`
      `/frem` match WPF; unknown `/command` prints locally (never broadcast); admin/moderator commands
      reach the server; a faked admin command is refused **by the server**.
- [ ] 🔴 **The soft keyboard COVERS the command bar instead of lifting it — NOT WORKING** (playtest-11).
- [ ] ➕ Chat **colours**, chat **tabs** (both exist in WPF), and chat **tags** — an icon between the
      time and the name: `[!]` for world, `[W]` for whisper. NOT BUILT.
- [ ] ➕ Chat **peek/fade when the log is hidden** (owner, 2026-07-24) — last 3-5 lines flash at the
      chat's spot for ~3-5s then fade, filtered by the active TAB; a pin toggle makes them persistent.
      NOT BUILT.

## ✅ 10. Reconnect and logout — VERIFIED 2026-07-24

A dropped cable/Wi-Fi shows `Connection dropped — reconnecting …` → `Reconnected — restoring session …`
→ `Session restored.` and puts you back on the SAME character in world; movement/attack still work
afterwards, proving the server re-associated the new connection id.

## ✅ 8. Resilience — VERIFIED 2026-07-24

Killing the server reports `Disconnected: …` in red and clears the world without crashing; restarting
it lets you log all the way back in; backgrounding the app recovers or reports; URL + username persist
in PlayerPrefs; the frame counter never shows `STALLED` while the server is healthy.

## 11. ✅ The uGUI client (0.28.x) — VERIFIED 2026-07-24, except Skills→Learn

Text renders (TMP resources present), buttons respond without walking the character, the skill bar
holds/moves/removes/auto-marks, passives can't be barred, the cast bar and all three cancel paths work
(with the 0.35s double-tap grace), the move marker clears correctly, name colours + zone discs + floating
damage are right, NPCs can't be damaged, auto-hunt fights, the bag equips/uses, and windows drag and
close newest-first.

- [ ] 🔴 **Skills window → Learn does NOTHING** (playtest-11). Action / To-bar / Use all work; **Learn
      alone is dead.** See also §12's Learn-confirm item, which cannot pass until this is fixed.

## 12. ✅ Parity batches A–F (0.28.41) — VERIFIED 2026-07-24

Auto-potions and auto-farm settings save and survive a relog; mob Info shows the full stat sheet + rank
and caches the drop table correctly; the bag's rows, Details, Compare and Bin all behave; vendors buy
and sell through the numpad with Max and per-item back-out; trade and party work with the bot as a
second player. **Exception:** the Skills→Learn confirm window is blocked by the §11 Learn bug.

## 13. ✅ Playtest-10 + features (0.28.42 → 0.28.55) — VERIFIED 2026-07-24, except the 3h banner

Speed changes no longer rubber-band; sit/stand behaves; the three flat HoT potions + the instant potion
tier correctly, with independent cooldowns and the Potions tab; potions work from the quick-use bar;
the bag's Equip paper-doll + A/B/C presets save, re-wear and survive a relog; the Hollow Crypt
gatekeeper lands you at the entrance; region banners and non-overlapping field/town outlines draw;
Menu → Rank shows all five boards; target and bar action buttons dispatch.

- [ ] ⏳ **The ~3h "take a break" banner is UNTESTED** — needs 3 hours of continuous play.

## 14. ✅ World pass — fields, walls, negative quadrant (0.28.56 → 0.28.61) — VERIFIED 2026-07-24

Filled colour fields (green→red by level) wrap each town with the town as a neutral island; Training
Grounds, Sunken Vale and Hollow Crypt all read as fields; boundary banners fire; you cannot walk off
the overworld into negative space or reach the jail on foot; the gatekeeper teleports into the dungeon;
the dungeon confines movement; `/jail` pins a character to the cell.
**Three defects found inside this area — see §17 items 1-3.**

## 15. ✅ Rune shots (0.28.62 → 0.28.64) — VERIFIED 2026-07-24

Boxes open into runes, the buff appears on the bar with a countdown, physical damage roughly doubles
with a soulshot and spell damage/cast speed rise with a spiritshot, wrong-type does nothing, runes
refuse deletion and trade while 1h/2h boxes trade, the wall-clock expiry survives a relog, and the
class-agnostic starter kit hands out its four choice boxes.
**Two defects + the economy findings — see §17 items 14, 4-6.**

## 16. Bag box-opening + item details (0.28.65 → 0.28.66) — NOT YET TESTED

Shipped after the last docs pass; not covered by the 2026-07-24 playtest.

- [ ] **Open a box from the inventory** — a plain box grants its contents straight to the bag; a
      **selection** box opens the choice popup and grants only the picked entry (0.28.65).
- [ ] **Item details layout** — the stat block is no longer crammed under the item name; long names
      and full stat sheets both lay out cleanly (0.28.66).

---

## 17. 🔴 OPEN — playtest-11 findings (2026-07-24, 0.28.66)

Full authoritative list with the owner's own wording lives in memory `playtest-11-queue`. Retest each
of these once fixed. Nothing here has been built yet.

**Bugs**
1. [ ] `/jail test1` then `/tp test1` teleports to the **dungeon, not the jail** (position clamping —
       both live in the negative quadrant).
2. [ ] **Mobs don't attack inside the dungeon** when you're displaced/teleported in from the debug
       menu — no aggro, no retaliation.
3. [ ] **Mobs are clamped together** in the crypt (bunched on one spot).
4. [ ] **Soft keyboard covers the command bar** instead of lifting it (§9).
5. [ ] **"Test1 entered the world" leaks to non-friends** — shown while a request is only `[pending]`.
       Entry/exit notices must be **mutual friends only**; keep the rest debug-only.
6. [ ] **`[info]` shows only for monsters/bosses, never for a player target** — and the 0.28.55
       player-target button grid comes OUT. Clarified 2026-07-24: "commands as buttons" means entries
       in the Skills window's **ACTIONS tab**, draggable onto the **skill bar** — not target-frame
       buttons. Retest with item 29.
7. [ ] **Debug-menu chat spam** — 10 potions print 10 lines. Drop the system messages for debug
       items/buffs/levels; keep the rare ones (tp coords, karma cleared, change class).
8. [ ] **`[lead]` doesn't update the party `*` flag** or remove the `[lead]` button; change `*` to a
       **star or crown**.
9. [ ] **Duplicate town-entry text** — a blue line under the big banner; remove the old one.
10. [ ] **`isAdmin` is per-CHARACTER, not per-ACCOUNT** — a non-admin character in an admin account can
        run admin commands.
11. [ ] **Skills → Learn does nothing** (§11).

**Changes**
12. [ ] **Stand-up delay** rules (§5).
13. [ ] **Bag: `Equip` button first**, equip column **expands LEFT**.
14. [ ] **Spiritshot buff reads `719h59` instead of `29d`** — duration needs day rollover.
15. [ ] **Admins must be excluded from the ranking system** (an admin at level 999 breaks every board).
16. [ ] **Shop items need details + buy-time info** — a soulshot shows no "works ONLY on PHYSICAL" text
        anywhere.
17. [ ] **Shop prices far too cheap** — equipment from **200g** minimum; runes **150k / 1h** and
        **280k / 2h** (confirmed 2026-07-24: two 1h = 300k, so the 2h carries a ~7% bulk discount).
18. [ ] **Show raw attack/cast speed numbers, not just the multiplier** — `1234/1500 (x3)`,
        `333/1999 (x1)` rather than a bare `x1.1` / `x0.5`.
19. [ ] **No HoT floating text for potions.**
20. [ ] **Target window numbers** — mobs: current/max HP as digits; players: the same **plus an MP bar**.
21. [ ] **Party window** — buffs/debuffs as **squares to the right** of each member (like the buff bar
        but no duration text, still flashing under 60s) to cut the height; **loot proposal as a
        drop-down** (or tap the blue "random" to open one).
22. [ ] **World border** — an orange dashed line like the jail's. It is the **fallback for where there is
        no physical collision marker**: something that says "this is the end, you cannot go further".
        Not a substitute for the collision in item 23. (Mountains/ocean later.)
23. [ ] **Real impassable WALLS — a CLIENT/SERVER split** (clarified 2026-07-24).
        **Client = collision:** you press against a wall and **stop at the surface**; the client never
        emits out-of-world coordinates, and a **tap outside your current world is rejected before it
        becomes a move order**. *This half doesn't exist yet — it is the work.*
        **Server = prevention:** the existing rubber-band (`ConfineToDomain`, `GameLoopService.cs:712`)
        **stays as the anti-cheat backstop** — do NOT weaken it. Today's snap-back is the symptom of the
        missing client half, not a bug in the clamp.
        Crossing between worlds stays **teleport-only**. Full design: memory `worlds-and-collision-design`.
24. [ ] **Target a party member with NO range restriction** so move-to/assist/heal/buff still resolve
        out of view and **kick / change-leader work from the action buttons**. Minimal frame — no HP/MP
        bars, or empty ones.
25. [ ] **Buff tap behaviour** — press-and-HOLD cancels, a single tap opens a details popup that closes
        on an outside tap. Holding a DEBUFF shows its details instead (debuffs can't be dismissed).
### 🔴 0.30.0 — the big batch (nothing below has been played)
26a.[ ] **Rogue uses BOTH weapons** — make a Fighter → Rogue. At 20/24/28/32/36 the Learn tab must
        offer BOTH the dagger ladder (`PiercingStab`) and the bow ladder (`PreciseShot`). Marksman /
        Warden / Hunter must NOT appear as 2nd classes. Bow range should still grow with tier.
26b.[ ] **Gear quality reads off COLOUR, not the name** — a vendor stocks the same piece at Common /
        Uncommon / Rare. Names identical, colours differ, and the item details show a `Rarity:` row
        with the % power. Check the inventory, both vendors, the warehouse and the worn squares.
26c.[ ] **Shop prices bite** — F/E/D only, nothing above Rare for sale. An F body ≈ 18k at Rare,
        6.3k at Common. Two vendors now: **Armsmaster** (weapons) and **Outfitter** (armor/jewels).
26d.[ ] **Vendor Detail/Compact toggle**, and the BUY confirmation shows full stats + description.
26e.[ ] **One aggressive mob per field** — walk a 22-28 field. Exactly one creature type should
        attack on sight; the rest ignore you until hit. Dungeons stay all-aggro.
26f.[ ] **Quest markers** over NPC heads: gold **!** takeable, gold **?** hand in now, grey **?** in
        progress. They must appear the moment you LEVEL into a quest, without relogging.
26g.[ ] **Track / Abandon** — [Track] pins to the on-screen panel (max 5, draggable, hides when
        empty). [Abandon] confirms first and the quest really goes.
26h.[ ] **Apothecary daily** — Miren gives a 1h shot box, levels 6-75, once per server day. The
        reward box must be UNTRADABLE; the ones she sells are not.
26i.[ ] **Town layout** — Brackenford: shops + Keeper east, class/quests west, Gatekeeper top-centre,
        Mindwright bottom-centre. **Every** other town has buffer / keeper / 3 vendors / gatekeeper.
26j.[ ] **The five-city world** — Brackenford 1-16 · Stonewatch 16-40 · Greymarsh 40-60 ·
        Ironreach 60-75 · Frostmere 76-90. Emberfall and Duskvale must be GONE from the map and from
        every gatekeeper's travel list. **Grandmaster Thorne is in Greymarsh**, not Brackenford.
26k.[ ] **86-90 exists** — the Frostmere 85-90 field must actually spawn mobs at 86-90 (not all 85),
        and each Frostmere field's elite spawner should be close but not aggro you from the camp.

### 🔴 0.33.0 — the world PLAN (camps, gates, managing cities)
27a.[ ] **A camp holds ONE band, not a level spread** — stand in the level 1-4 camp west of Brackenford.
        Every creature must be Ridgeback Pup or Fox, levels 1-4. **No Werewolves** (they are level 12 and
        belong two camps away). Then check the 4-8 camp: Fox and Goblin Scout only.
27b.[ ] **Camps are visibly separate** — walking from one camp to the next crosses ~1000 units of empty
        ground, and nothing from the higher camp follows you into the lower one.
27c.[ ] **Fields are off the town wall** — leaving Brackenford by any gate you walk ~1500 clear before the
        field colour starts. Stepping out of town must never be stepping into a camp.
27d.[ ] **The gatekeeper lists NAMED field gates** — talk to Gatekeeper Pell. Above the other cities there
        should be a group per field ("Bracken Hollow", "Bracken Downs") with a row per camp, each naming its
        band and its creatures ("Lv 8-12 · Goblin Scout, Ashen Wolf, Werewolf"). Levels and fee shown.
27e.[ ] **Travel lands you at the gate, every time** — take the same gate twice. Both times you arrive on
        the camp's **town-facing edge**, in the same spot (±150), looking into the camp. Never inside it,
        never somewhere different.
27f.[ ] **Each city offers its OWN fields** — Stonewatch's gatekeeper lists the three Stonewatch fields
        (16-24 / 24-32 / 32-40) and NOT Brackenford's or Frostmere's.
27g.[ ] **Death returns you to the managing city** — die on the far edge of a Stonewatch field (the side
        facing Brackenford). You must wake in **Stonewatch**, the city that owns the field, not in whichever
        town is closest. Dying in open ground between cities still uses nearest-town.
27h.[ ] **The elite camp is a choice** — each Frostmere field's elite camp sits ~1500 beyond the top camp.
        You should be able to farm the normal camp right up to its edge without the elite aggroing, and
        have to walk in on purpose.

25b.[ ] **No combat-logging out of a DoT** — while a bleed/poison/venom is on you, "character select"
        must REFUSE with "You can't leave while in combat" and you stay in the world. Once the DoT ends
        (and combat decays) it works. Same for `/exit`. Pulling the plug mid-DoT must not run the
        link-dead grace down.
25a.[ ] **Buffs survive a relog** — take buffs, note the timers, exit to character select and re-enter:
        they come back with LESS time, not full time and not gone. Wait a few minutes offline and the
        clock should have kept running. A buff that ran out while away must NOT reappear. Runes
        (soul/spiritshot) must appear exactly ONCE, not twice. Switching to another character on the
        same account must show that character's buffs, not the first one's.
26. [ ] **"You entered <field>" needs hit-test FALSE** — it currently blocks tapping the ground beneath
        it (click-through family).
27. [ ] 🎯 **Partial-stack trading** — "5 of my 15 sticks for his 10 of 16 stones". **The owner's answer
        to the open design call: YES.** `TradeOffer` must carry per-item counts and split on completion.

**Additions**
28. [ ] Chat **colours**, **tabs**, **tags** (§9).
29. [ ] **Every non-admin command as an ACTION** — friend add/remove/list · party
        invite/kick/changeleader · sit/walk/run · attack/assist/nextTarget. Clarified 2026-07-24:
        these live in the Skills window's **Actions tab** and must be **placeable on the skill bar**,
        exactly like a skill. Not target-frame buttons (see item 6).
30. [ ] **Block system** — `/block <name>` (all chat forms ignored; sender sees "<me> person has blocked
        you"; permanent), `/unblock <name>`, `/blocklist`.
31. [ ] **Charisma system** — `/like` (+1, 20/day, never negative); killing costs `karma × 0.01`; every
        20 charisma = **+1% exp/sp drop, capped 1000 = +50%**; chatban −20/h, jail −100/h, kick −250/h,
        ban zeroes both; **two values** — a 0-1000 bonus pool and a lifetime total for ranking.
32. [ ] **Buy-back menu** — last 10 deleted/sold items; deleted or sold-for-0 restore via `[r]`, sold
        for >0 buy back at price.

**Confirmed work (was "not sure", resolved 2026-07-24)**
33. [ ] **Starter-gear redesign** — the best 0-20 items one-shot everything. Newbie boxes become a
        **level-10 QUEST** (levelling to ~15 along the way); levels 1-10 get the **weakest gear in the
        game** — training weapons at 400g, training armor, **no shots or jewels**; broken jewels drop
        from level 1-5 mobs and sell cheap. Full numbers in memory `playtest-11-queue`.
34. [ ] 🔴 **Levelling curve** — the mob-XP question is answered: it is **neither** the L2 formula nor a
        per-mob value. `ExpToNext = 25L²` (quadratic) vs `MobExpReward = 40 + 35·L` (linear), with a
        toughness multiplier from the mob's HP as the only per-mob variation. That shape gives **~2 200
        kills for all of 1→80** at `ExpRate 1` (~220 at the current ×10), and only ~56 kills/level at
        endgame. **Needs an owner decision on the curve**, ideally together with item 33. Measure any
        change with `tools/BalanceMatrix` — never by hand.

---
## Known gaps (NOT bugs — not built yet in the Unity client)

Don't file these; they're scope, not defects. The Unity client is a viewport, not the WPF harness.
As of 2026-07-23 the A–F parity batches closed most of the old gaps (inventory, skills, skill bar,
buffs, quests, party, trade, vendors, NPC dialog, debug, auto-hunt, mob info all exist now, on
uGUI + TextMeshPro). What remains:

- **Enchant / reroll UI** — deferrable per the owner; not built.
- **3-tab auto-potions** (4 potions + buff-potions/scrolls) — **blocked on the potion rework**
  (Roadmap, 2026-07-23). Today's window is HP/MP % only.
- **Per-skill auto-farm priority / custom-cd / cyclic toggle / heal chain** — deferred design
  (Roadmap, 2026-07-23); today auto-farm uses the server's priority-scan and skill-bar Auto marks.
- **Clock** and the **§8 target slash-commands** (`/ptinv` etc.) — deferrable.
- Entities are coloured billboards, not models; no animation (waits on the art pass).
- Portrait layout is not supported; the UI is authored for landscape.
