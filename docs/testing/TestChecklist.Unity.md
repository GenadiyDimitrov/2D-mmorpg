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
### ✅ 0.30.0 — the big batch — VERIFIED ON DEVICE 2026-07-31 (playtest-15)
All of **26a-26k** confirmed: rogue learns both ladders, gear rarity reads off colour, shop prices bite,
one aggressive type per field, quest markers + tracker, the apothecary daily, town layouts, the five-city
world and the 86-90 band. **One finding carried forward** → §32.

### ✅ 0.33.0 — the world PLAN — VERIFIED ON DEVICE 2026-07-31 (playtest-15)
All of **27a-27h** confirmed: one band per camp, camps visibly separate, fields off the town wall, named
field gates, a repeatable landing spot on the town-facing edge, per-city field lists, death returning you
to the managing city, and elite camps you have to walk into on purpose.

### ✅ 0.33.1 — the ADMIN menu — VERIFIED ON THE PHONE 2026-07-31 (playtest-15)
**28a-28d** confirmed on the release server: every admin tab does something, a plain character sees no
Admin row and no gap, admin commands are refused with a message, and a fresh DB bootstraps exactly one
admin. **28e could not be tested — there is no delete button in character select at all** → §32.

### ✅ 0.34.0 — per-mob spawners + F-grade drops — VERIFIED ON DEVICE 2026-07-31 (playtest-15)
All of **29a-29e** confirmed: quest mobs respawn per-mob so a target is always available, the fuller camps
still pull one at a time, level 1-17 mobs drop Ferrite gear with the right rarity gates, early drops are
upgrades on the re-cut Training armor, and the server log names no unserved quest target.

### ✅ 30. THE ECONOMY REWORK (0.34.1/.2) — VERIFIED ON DEVICE 2026-07-31 (playtest-15)
**30a PASSES — the faucet is closed.** A mage reached level 25 in ~2h with **~1kk gold, potions and
scrolls UNSOLD** (vs 3kk of pure trash gold on 0.33.1); his words: *"now seems fine and will be better
when we fix the drop logic"*. **30b, 30c, 30d, 30f, 30g, 30h, 30k** all confirmed: one piece per slot
family per kill, family flavour gone for gear, grade-locked drops, the potion/scroll/mat/jewel groups,
and the displayed rate matching what he observed. **Still open** → §32.

30i.[ ] **An elite pays a band better than trash** — the dungeon elites (Hollow Crypt, 44-48) and the
        generated elite camps drop **no Common at all**: Uncommon 10%, Rare 2%, Epic 0.2%.
30j.[ ] **A boss pays out properly** — the Grave Lich (48) / Valley Treant (60) should drop **several**
        pieces: Epic 70%, Legendary 40%, Mythic 2%, per slot family. This is a NERF from the old flat
        50% Mythic body — say so if it now feels thin.
30l.[ ] **`/droprate` works and tunes live** — type `/droprate` for the current table, then
        `/droprate gear 1` and confirm gear drops roughly TRIPLE without a restart (it removes the ×1/3
        the gear groups ship with). Put it back with `/droprate gear 0.333`. Also try
        `/droprate mats 2` — mats must double while nothing else moves. **This is how you tune the
        economy during the playtest instead of asking for a rebuild.**

### ✅ 31. FEEDBACK / WORDING (0.34.3) — VERIFIED ON DEVICE 2026-07-31 (playtest-15)
**31a, 31b, 31c, 31d, 31f** confirmed: the reuse overlay drains and clears on time, ESC pays the cooldown
while an enemy interrupt does not, consumables carry their own timers, no stale timer survives a relog,
and the Learn confirmation shows its numbers. **31e is right but unreadable** → §32.

--- 32. 🔴 PLAYTEST-15 (2026-07-31, server 0.34.3) — full report: `Playtest-15.md`. ---
**BUILT IN 0.35.0 — test these:** 32a · 32b · 32h · 32i · 32j · 32k · 32l · **32m** · 32o · 32p · 32s · 32v.
(32m and 32s came out of the SAME line — see 32s. 32m was scheduled for the Unity batch and arrived early.)
**BUILT IN 0.37.0 — test these:** 32c · 32d · 32e · 32f · 32g · 32n · 32q · 32r, plus **32w** (the NPC
buffer's new basic-only list). All but 32f/32g/32q are Unity-side, so the APK is what proves them.
**BUILT IN 0.38.0 — test this:** 32t (jewel slots). Server + Unity paper-doll.
**BUILT IN 0.39.0 — test these:** 32u (free travel under 40) + **32z** (the auto-farm skill chains,
playtest-15 big design #1). §32 is now fully built.
32a.[ ] **The phone server starts with no hand-editing** — unzip a fresh `Game.Server` on the phone and
        `dotnet Game.Server.dll` must just run. It currently dies with `GC heap initialization failed
        (0x8007000E)` — Server GC tries to reserve 256 GiB — and he has to `nano
        Game.Server.runtimeconfig.json` and flip `System.GC.Server` to false after EVERY update.
32b.[ ] **Class change applies without a relog** — finish the class-change quest, take the change at the
        class master: the class must update immediately, and the Skills window must show the new
        unlearned list at once (today: no update, relog required, then a further delay).
32c.[ ] **The set bonus lists its pieces** — the set bonus is shown but not which equipment the set
        requires and which slots are filled. That listing is missing entirely.
32d.[ ] **Vendor details for stackables** — tapping a quantity item in a vendor must open the details
        window first, not jump straight to the numpad (carried from 26d).
32e.[ ] **Character select has a delete button** — there is none at all today, which is also why 28e
        (the admin's fast delete + 10s undo) has never been testable.
32f.[ ] **The drop list reads as a tree** — group is a TITLE line carrying the group name and the group
        %, with its item rows INDENTED beneath it. Flat and clustered today (carried from 30e).
32g.[ ] **Passive numbers are grouped** — the mage's weapon proficiency currently reads "+cast, −cast,
        +cast …". Same-stat entries must be gathered together (carried from 31e).
32h.[ ] **HP potions drop less** — infinite potions make you unkillable. He still takes real damage and
        had to use vampiric to survive, so it is the potion FAUCET to close, not the damage.
32i.[ ] **Nuker has no Wind Walk** — it is a self buff that stacks with other buffs and should not be on
        the class. Same for the rogue's **Battle Fury** — not in the original CSV.
32j.[ ] **Starter gear numbers** — Training (Wooden) shield 35 def · Ferrite Aegis 90 pDef at Mythic ·
        ALL training weapons show 5 mAtk · training wand pAtk 6 / mAtk 7 and **no +6 maxMP**.
32k.[ ] **Auto-farm retaliates** — a mob that is hitting you outranks the nearest one as a target. He was
        being ganked by orc archers while the autopilot kept killing the nearest thing.
32l.[ ] **NOTHING walks you into melee unless you commanded it** (widened from "casters" — the owner's
        rule has no class in it). Check all four:
        · **Auto-farm** with the Attack action NOT on the bar (or on it but auto-OFF): stands still with
          the target selected; an active skill still closes to CAST range only. With it auto-ON it may
          melee. This holds for a FIGHTER too, not just a mage.
        · **A skill alone never closes.** Press an offensive skill on a distant mob having given no
          attack order: you cast (walking only as far as cast range) and then STAY there. Try it as a
          fighter and as a bow rogue — both used to charge in after the cast.
        · **The melee combo survives**, because that one IS commanded: tap-tap a mob (you approach and
          swing), then fire a skill at the SAME mob → you resume auto-attacking afterwards.
        · **A walk order or a follow cancels the standing order**: after tapping the ground, a later
          skill must not resume the melee.
32m.[ ] **Tap-to-target, tap-again-to-attack — and the Attack button means exactly the same thing.**
        First tap only opens the target window; a second tap on the SAME target attacks (melee weapon →
        walks in; bow → shoots from range), a party member → follows. Then do the identical checks with
        the **Attack action on the skill bar** and the **Attack button on the target frame** — all three
        are one code path (`GameBoot.AttackOrFollow`) and must not differ.
32n.[ ] **Consumables show a count on the hotbar** — 1, 2, 3 … 98, 99, then `99+`.
32o.[ ] **Escape/return scrolls can be sold** — they are tradable but the vendor refuses them.
32p.[ ] **Buff potions sell at ÷25 like everything else** — 1500/25 = 60, not 450.
32q.[ ] **Auto-farm and offline farming show their remaining time** — buff-timer format (`24h00m01s` ==
        `1d`), on the button when enabled, or one chat line on every on/off change.
32r.[ ] **The farming-range circle only shows when the range toggle AND auto-farm are both on.**
32s.[ ] **Your own party can NEVER be hit — and the tap follows them instead.** Tap a party member
        twice: you must start FOLLOWING ("You follow X."), never swing. Try it with PvP ON, and with a
        RED party member — both must still refuse, with "You can't attack a member of your own party."
        Try an offensive SKILL on them too (the rule lives in `CanPvpHit`, so every route inherits it).
        Then confirm a NON-party player CAN be attacked on the second tap when PvP is on and you are
        out of town — that half was genuinely broken: `TouchInput` only sent an Attack for a MOB, so
        tapping any player did nothing at all.
32t.[ ] **Jewels have designated slots** — the paper-doll shows **Neck · Ear · Ear · Ring · Ring**, and an
        empty square names its slot. (FIVE slots, not six: the report's "pendant" is the necklace —
        the necklace-family items are literally *named* Pendant in the catalogue.) Equipping swaps
        like gloves do — a third ring must NEVER be refused with a message. Swap rule: replace the
        WEAKER of the two; if both are the same rarity replace **slot 1**; empty counts as weaker
        than Common. Walk his worked example: no rings → common goes to slot 1 → 2nd common to slot 2
        → a **rare** replaces slot 1 (tie) → an **uncommon** replaces slot 2 (weaker) → another
        uncommon replaces slot 2 again. Slot 1 always holds the stronger of the pair. Then **relog**:
        the pair must come back in the same two squares (the slot is derived, not stored).
32u.[ ] **Free teleport under 40** (0.39.0). At level 39 every gatekeeper row must read **"Free"**, not
        "0 gold", and the ride must actually cost nothing (watch the gold counter). At 40 the distance
        fee is back, minimum 50 — quote and charge must match, so read the row's price and then check
        the gold you lost. Both the local field gates and the other cities.
32z.[ ] **Auto-farm skill chains** (0.39.0, playtest-15 big design #1). In the **Auto Farm** window:
        · **Cyclic OFF** (default, "first available") — put a short-reuse skill in slot 1 and two
          slower ones after it: the order should read 1-2-1-3-1-2.
        · **Cyclic ON** — the same bar must read 1-2-3-1-2-3: after casting 1 it does NOT go back to 1
          until the others have had their turn. (It does wrap early if every later skill is still on
          cooldown — that is deliberate, the alternative is standing idle.)
        · **Priority** — with a heal, a buff and an attack all auto-on, a needed heal always beats the
          buff, and the buff always beats the attack.
        · **Heal threshold** — set it to 50: nothing heals above 50% HP, and below it the heal chain
          takes over until you are back over. Set it to **100** on a healer: heals fire on cooldown,
          and in a party they land on the most injured member in range, not always on yourself. Turn
          the heal row **off**: heals never auto-cast at all.
        · **Buff renewal** — an auto-buff must be recast when it drops under **60s**, not only when it
          has expired, and must NOT be recast while a stronger version of the same family is running.
        · **Debuff** — cast a rank-1 debuff, then learn/press the higher rank: the chain must replace
          it rather than treat "something with that key is on the mob" as done.
        · **Assist party leader** — in a party you don't lead, with it ON: you attack only what the
          leader attacks, you follow him onto a new target, and when he has none you stand still and
          your target frame is empty (no roaming, no picking your own, no chasing what hit you).
        · All of it must survive a **relog** (it rides in `AutoHuntJson` — no db reset).
32x.[ ] **An improved buff is ONE square on the bar.** Cast the cleric's Improved Speed: exactly one
        square named "Improved Speed" (0.36.0 put up four). Its timer must be the SHORTEST part, the
        tap popup must list the parts with their own times, and press-and-HOLD must remove the WHOLE
        group — no leftovers. Then check a potion/scroll square is unchanged: a Swift potion still
        reads "Swift", NOT "Swift Potion (Greater)".
32w.[ ] **The NPC buffer gives BASIC buffs only, one hour each.** Its list must be Might · Force ·
        Focus · Body · Frenzy · Swift · Alacrity · Agility · Haste — **no "Improved Speed" group and
        no Harmony of Protection / Warrior / Wizard**. Each lands as ONE buff square at 1h. Check the
        overlap rule still holds: take Swift from the buffer, then drink a Swift potion of the same
        tier — the potion must be REFUSED (not eaten) because the buffer's hour is longer; a HIGHER
        tier potion must replace it. Full-set price is unchanged (nine buffs).
32v.[ ] **Auto-farm shows its target** — while the autopilot is running, the target window must show the
        creature it is currently on, update as it switches, and clear when it has none. The server
        already picks one (`GameLoopService` sets `CombatTargetId` in the auto-hunt path, ~:3043/:3056);
        what's missing is that your own client is never told, so the frame sits empty or stale. Pairs
        with 32k — you can't see it retaliate if you can't see what it's chosen.

32y.[ ] **One item can be tuned on its own** (0.38.1, playtest-15 big design #3 — the group MATH was
        already what you described, so only the per-item knob is new). On the phone, as admin:
        `/droprate item Scroll of Resurrect 5` — it must accept the NAME, not just the id, and confirm.
        `/droprate` with no args must then list it under "Per-item overrides". Inspect a mob's drop
        tree: the Always group still reads 100%, but its members now print their individual SHARES and
        the scroll's must have grown at the others' expense — **the group must NOT fire more often**,
        because it was already guaranteed. `/droprate item Scroll of Resurrect 1` clears it and the
        tree goes back to a plain name list. A wrong name must suggest near matches, not fail silently.

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
