# Test Checklist — Unity client (Android / Editor)

Companion to `TestChecklist.md` (which covers the WPF client + server behaviour). Same
conventions: **`[ ]` = not tested, `[x]` = verified, `[~]` = tested, needs a change/tuning.**

The Unity client is a **thin view over the same server** — it shares `Game.Shared` and speaks the
real protocol through `NetworkChannel`. So most items here test the CLIENT, not the game: if a
number is wrong in both clients it's a server bug and belongs in `TestChecklist.md`.

**Test in this order.** Every section depends on the one above it — a failure at step 2 makes
everything below it meaningless, so stop and report at the first ✗ rather than ticking on.

---

## ⚙ First-run setup (do this once per PC reboot / fresh checkout)

- [x] **`activeInputHandler` = `2` (Both)** — was `1` (New Input System only), which made legacy
      `Input.*` throw every frame so **tapping did nothing**. Baked into the build since `5d35e80`.
- [ ] Server is running (`dotnet run --project Game.Server`), listening on `http://localhost:5238`.
- [ ] **Cabled phone:** `adb reverse tcp:5238 tcp:5238` has been re-run **since the last reboot/unplug**
      (the tunnel does not survive either), and the URL is `http://127.0.0.1:5238/game`.
      **Same Wi-Fi instead:** use the PC's LAN IP and make sure the firewall allows 5238.
- [ ] adb: `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`

**The Editor is no longer needed to ship a build.** `Assets/Editor/CommandLineBuild.cs` builds the APK
headlessly with Unity CLOSED (it must be — the project lock), so Claude can build, `adb install -r`,
launch, `screencap` and `logcat` without the owner opening Unity. Only the *owner* closing Unity is
required. `builds/L2Clone.apk` is the one and only APK (the older `L2CloneMmorpg.apk` was deleted
2026-07-21). There is a single Android package — the URP template default
`com.UnityTechnologies.com.unity.template.urpblank`, labelled **`Game.Client.Unity`** on the phone —
so every build installs OVER the last one regardless of the output filename; a second icon is not
possible until someone changes the bundle id.

---

## 1. ✅ It starts and tells you something — VERIFIED ON DEVICE 2026-07-21

Status strip, Sign in panel, finger-sized scaling, soft keyboard, and the Log console all confirmed by
screenshot on the S23 Ultra. The blocker that hid all of it was **IL2CPP stripping** — see
`Assets/link.xml`, and **do not delete it**: stripping breaks the phone build ONLY, so desktop testing
will never catch its return.

## 2. Connection — honest failure first

Test the failure case **deliberately**; a client that only works when everything is perfect taught
us nothing last time.

- [ ] **With the server STOPPED**, tap Login → a red error appears in the panel and the log, naming
      the refusal and reminding you about `adb reverse` / LAN IP. It must NOT hang silently.
- [ ] The dot stays **red** and the phase stays `Offline`.
- [ ] Start the server, tap Login again → it connects with no restart of the app needed.
- [ ] **Register** with a fresh username creates the account (then reuse it with Login).
- [ ] A wrong password gives a clear server error, not a crash.
- [ ] **Version handshake:** the client sends `GameConstants.GameVersion`. (To prove it, bump the
      server's version, rebuild the plugin DLL, and confirm login is refused with "Client out of date".)

## 3. Character select

- [ ] After login the panel switches to **Characters** and lists the account's characters with
      `Name / Lv / Race / Class` — the SAME characters the WPF client shows for that account.
- [ ] A fresh account shows "No characters on this account yet."
- [ ] **Create character** — name field, tap-to-cycle Race (Human → Elf → Ork, **no God**) and Class
      (Fighter → Mage). Create returns to the list with the new character on it.
- [ ] A duplicate/invalid name shows the server's error message rather than failing silently.
- [ ] **Enter** puts you in the world; the phase strip changes to `InWorld`.
- [ ] **`Account: <name>`** is shown under the "Characters" heading.
- [x] **Logout** (next to "Create character") returns to the **Sign in** panel, the phase goes
      `Offline`, and the password field is empty. Verified on device 2026-07-21, including logging
      back in afterwards.
- [ ] After a Logout it does **not** silently sign you back in — the reconnect handler's cached
      credentials must have been cleared. Wait ~30s on the login screen to be sure.
- [ ] Logging in as a **different account** after a Logout shows THAT account's characters, not the
      previous one's (the connection is dropped on logout, so no server session can leak across).

## 4. The world renders — this is what was broken

The client used to subscribe to `"Snapshot"` while the server only sends `"SnapshotDelta"`, so the
world was **permanently empty**. These items exist to prove that path end to end.

- [x] 🔴 **Your own entity is there the moment you enter** — the self panel shows name/Lv/HP/MP, NOT
      `waiting for your entity …`. **This regressed on 2026-07-21** (mobs rendered, you didn't) and was
      **verified fixed on device the same day** (self panel showed `Admin Lv 90`, HP/MP, pos).
      Cause: `EnterWorld` cleared the world *after* awaiting the reply, so the first frame — the only
      one that ever carries your full DTO — was wiped, and a standing player is byte-identical every
      tick so the server never re-sends it. Mobs recovered only because they wander out of view and
      respawn. Fixed by clearing *before* the request + `EntityManager.SetSelf`.
- [ ] **Resync safety net:** if the self panel ever does stall, the log must show
      *"World state out of sync — asking the server to re-send it"* and the world must repopulate
      within ~2s. **Seeing that line is itself a finding** — it means the race still happens and only
      the net is saving it. Report it rather than shrugging at a working screen.
- [ ] Enter → **Leave** → Enter again, three times in a row: the self entity appears every time
      (this is the timing race, so a single pass proves little).
- [ ] The status strip shows **`frames N @ ~10.0/s`** and the dot is **green**. 10/s is the server
      tick reaching the phone.
- [ ] **`entities` is > 0** and roughly matches what the WPF client sees standing in the same spot.
- [~] **You** are a green billboard, with the camera on you. **2026-07-21: everything was MAGENTA** —
      `Shader.Find("Unlit/Color")` returns null in a URP *player* build, so the primitives kept Unity's
      missing-shader material. Fixed via `UnlitMaterials.cs` + Always Included Shaders; **re-test.**
      Nameplate text colours were always right, so only the quads were affected.
- [~] Mobs are **red**, NPCs **yellow**, other players **cyan**. (Same magenta bug — re-test.)
- [ ] **Nameplates** float above every entity, with an HP bar under each (NPCs have no bar).
- [ ] An **aggressive** mob's nameplate ends in `*`.
- [ ] **Level privacy holds here too** — your own plate/panel shows `Lv`, another player's does not.
- [ ] Walk out of view range and back: entities **despawn and respawn** instead of piling up or
      vanishing permanently. (Delta bugs show up exactly here.)
- [ ] Kill something → the corpse **dims** rather than disappearing instantly.

## 5. Movement — "does it move?"

- [ ] Tap the ground → you walk there. **Watch the `pos` numbers in the self panel change** — that is
      the ground truth, independent of the camera.
- [ ] The **ground grid** slides past as you walk. (Without it, a follow-camera over flat ground makes
      walking look identical to standing still.)
- [ ] Movement is **smooth**, not teleporting once per tick (the views interpolate between updates).
- [ ] Open the WPF client on the same character's account and **watch the Unity player move from the
      WPF window** — proves the server, not just the client, believes you moved.
- [ ] **Walk/Run** toggles speed (`spd` in the self panel changes).
- [ ] **Sit/Stand** works and blocks movement while sitting.

## 6. Combat + HUD

- [ ] Tapping a mob targets it → the **target panel** appears top-right with name, level, HP bar, kind.
- [ ] The **Attack** button engages the current target; HP bars drain on both the target panel and the
      nameplate.
- [ ] Combat lines involving **you** appear in the log (`You → Wolf  Hit 42`), and only those.
- [ ] Your **HP/MP bars** drain and regenerate; regen arrives in **3-second chunks** (matches WPF).
- [ ] The **XP bar** moves on a kill and a level-up writes `Level up!` to the log.
- [ ] **Gold** in the self panel matches the WPF client.
- [ ] **Die** → **Respawn** button returns you to town.
- [ ] **Leave** returns to character select cleanly, and re-entering works without an app restart.

## 7. Input hygiene

- [ ] Tapping a **UI panel or button never also moves you** — most obviously: tap Login, then check you
      didn't queue a walk; in world, tap the action bar and confirm you stay put.
- [ ] Tapping the console's scroll area scrolls it instead of walking.
- [ ] After tapping **Send**, focus is dropped and the soft keyboard closes — the next tap on the
      ground must WALK, not get eaten by the text field.

## 9. Command bar, chat and slash commands

The chat field moved OUT of the log console into an always-visible command bar above the action bar.
Two IMGUI text fields sharing state fought over focus on mobile, so the console is now a pure viewer.

- [ ] The **command bar is visible in world** without opening the log, with a greyed hint in it.
- [ ] The soft keyboard **lifts the command bar** instead of covering it.
- [ ] Plain text → **local chat**, and the line reaches the **WPF client** standing nearby.
- [ ] **`!message`** → world chat (reaches a WPF client anywhere on the map).
- [ ] **`/w Name message`** → whisper; the target's WPF client shows it, nobody else does.
- [ ] **`/w Name`** with no message prints the usage hint locally and sends nothing.
- [ ] **`/fadd Name`**, **`/flist`**, **`/frem Name`** work and match what the WPF client's friend
      list shows for the same character.
- [ ] As a **normal player**, an unknown `/command` prints "Unknown command" **locally** — it must NOT
      be broadcast as chat text for the whole zone to read.
- [x] As an **admin/moderator** character, `/`-commands reach the server and take effect (an admin
      kicked a player from the phone, 2026-07-21). The login line in the log ends with
      `[Admin]` / `[Moderator]`.
- [ ] A non-admin who fakes an admin command still gets refused **by the server** — the client's
      `IsAdmin` is only an optimisation, never the authorisation.

## 10. Reconnect and logout

- [ ] Pull the USB cable / drop Wi-Fi briefly while in world → the log shows
      `Connection dropped — reconnecting …`, then `Reconnected — restoring session …` → `Session restored.`
      and you are back **on the same character, in the world** — not on an empty character screen.
      (SignalR's auto-reconnect gives a NEW connection id, and the server's session is keyed to the
      connection, so a reconnect leaves you connected but NOT authenticated.)
- [ ] After a restore, movement/attack still work — prove the server really re-associated you.
- [ ] **Logout from character select** (see §3) then log in again in the same app session.

## 8. Resilience

- [ ] **Kill the server while in world** → the client reports `Disconnected: …` in red, clears the
      world, and does not crash or freeze.
- [ ] Restart the server, tap Login → you get all the way back in.
- [ ] **Background the app** (home button) and return → it either recovers or reports the drop; no
      silent frozen world.
- [ ] Server URL and username are **remembered on next launch** (PlayerPrefs) — no retyping the IP.
- [ ] Leave it in world for a few minutes: the frame counter keeps climbing and never shows
      **`STALLED`** while the server is healthy.

---

## 11. The uGUI client (0.28.x) — NOT YET TESTED

The UI was rebuilt on uGUI + TextMeshPro on 2026-07-21. Test in this order; the first two decide
whether anything below them means anything.

- [ ] **Text renders at all.** If labels are blank, TMP's essential resources are missing and nothing
      else matters (`docs/unity/EditorSetup.md` step 1).
- [ ] **Buttons respond**, and tapping a panel does NOT also walk the character.
- [ ] Login is prefilled **admin/admin**. The app version and the server's may now DIFFER legitimately
      — since 0.28.22 the server accepts a LIST of compatible client versions, so a server-only fix no
      longer forces a reinstall. If login is refused as "out of date", the client's version is simply
      missing from `GameConstants.CompatibleClientVersions`.
- [ ] **Skills window** — Known/Learn/Actions tabs; learn with SP; "To bar" then tap a slot; the skill
      sticks across a relog (the server owns the bar).
- [ ] Passives can **not** be placed on the bar.
- [ ] **Hold a slot** → Move / Remove / Auto. Move + tap another slot swaps them. Auto shows a green
      frame and only appears for castable skills.
- [ ] **Cast bar** appears; tapping it cancels; the **X on the casting slot** cancels; **back** cancels.
      A DOUBLE TAP on a skill must not start-and-cancel it (0.35s grace).
- [ ] Tapping the ground mid-cast says "can't move while casting" and shows **no** marker.
- [ ] **Move marker** is small, appears where you tapped, and clears on arrival, on attack, on a cast
      and on sitting.
- [ ] **Name colours**: you green; players white / purple (flagged) / red (PK); mobs by level gap
      (red ≥9 above → grey trivial); NPCs yellow. Nameplate + HP bar sit ABOVE the marker.
- [ ] **Zone discs** on the ground and the zone readout under the self panel.
- [ ] **Floating damage** — crits gold and bigger, incoming red, misses shown.
- [ ] **NPCs cannot be damaged** by attack OR by skill.
- [ ] **Auto-hunt** actually fights (it needs at least one Auto-marked skill or basic attack).
- [ ] **Bag** — equip/unequip, Use on consumables.
- [ ] **Windows drag** by their title bar and stay on screen; **back** closes them newest-first, then
      offers to quit; quitting leaves the world and logs out before closing.

## 12. Parity batches A–F (0.28.41) — NOT YET TESTED (built 2026-07-23)

Everything below shipped in one build cycle to reach WPF *functional* parity. Test after §11.

**Auto-hunt (Menu → Auto Pots / Auto Farm):**
- [ ] **Auto Potions** — HP and MP each have an on/off (green ON / red off) + a threshold slider;
      **Save** persists; reopening shows the saved values (off keeps its number). Off = not used.
- [ ] **Auto Farm** — search-range slider, Keep-position toggle, Normal/Elite/Boss engage toggles;
      Save persists and survives a relog. **Reset** restores defaults.
- [ ] Toggling the top-right **Auto** button (or an Auto-marked skill) does **not** wipe the
      potion/farm settings you saved.

**Mob info (target → Info):**
- [ ] **Stats tab** shows the full sheet (attributes, offense, defense, utility, effects, traits) and
      the mob's **rank** in the title for elites/bosses.
- [ ] **Drops tab** loads the drop table the first time it's opened; switching Stats↔Drops does **not**
      re-request; **reopening** (Info again) re-requests. A player target has no Drops tab.

**Inventory (Bag):**
- [ ] Rows are `name (qty) [Details] [e|u]`. **[e]** equips/unequips gear, **[u]** uses a consumable,
      both without opening the window.
- [ ] **Details** shows stats (enchant-scaled), attributes, use-skill, and set info; per-kind actions
      (Use / Equip-Unequip / Bin).
- [ ] **Compare** (on an unequipped gear piece) opens the worn counterpart alongside, marked orange
      **E** — the two stat blocks read side by side.
- [ ] **Bin** — a stack asks *delete one / whole stack* (the reusable selection popup); a single item
      just confirms; the item is gone after.

**Vendors (talk to a vendor → Buy / Sell):**
- [ ] **Buy** lists wares with prices; unaffordable rows are dimmed. A stackable item opens the
      **numpad**; **Max** = the most you can afford; a non-stackable skips straight to confirm.
- [ ] **Sell** lists your sellable bag (equipped/quest items excluded); **Max** = the whole stack;
      the confirm shows the right total; after selling, the row updates and your gold changes.
- [ ] Numpad: digits, **C** clear, **<** backspace, the **number box** accepts the phone keyboard, and
      **X** backs out of that item (not the whole vendor).

**Skills → Learn:**
- [ ] Tapping **Learn** opens a **confirm** window showing the change (power/MP before→after for an
      upgrade, or the level-1 numbers for a new skill) + the SP/gold cost; **Cancel** spends nothing,
      **Confirm** learns it.

**Trade + party (needs the bot as a second player — [[bot-second-player]]):**
- [ ] Target another player → **Party** invites; **Trade** opens the trade window. Offer items + gold,
      both sides Ready → the swap happens; Cancel/close leaves the trade cleanly.

## 13. Playtest-10 + features (0.28.42 → 0.28.55) — NOT YET TESTED (VPN proof-of-concept only, 2026-07-23)

The 2026-07-23 evening test only proved the VPN download/play path. Everything below is unverified.

**Movement / sit (0.28.45, 0.28.53):**
- [ ] Speed changes (walk, slow, stun) no longer **rubber-band** — you predict at the right speed.
- [ ] **Sit** freezes movement; **standing** lets you walk immediately (no 3s freeze) but you can't
      attack/cast for the recovery window. Standing never yanks you back.

**Potions (0.28.48–0.28.49):**
- [ ] The three flat HoT potions (Common/Uncommon/Rare) heal a flat amount over time; a stronger tier
      **replaces** a weaker running one; the **instant** potion heals at once and does **not** cancel a HoT.
- [ ] Per-potion cooldowns are independent; **Auto Pots → Potions tab** picks a tier per HP threshold.
- [ ] A potion placed on the quick-use bar (`item:` token) drinks from any stack of it.

**Equipment presets + paper-doll in the bag (0.28.50, 0.28.55):**
- [ ] Bag **Equip** toggle widens the window and shows the worn-gear paper-doll column + A/B/C presets.
      Collapsing narrows it again. Tapping a filled square opens item details (Unequip).
- [ ] **Save** stores the worn set into A/B/C; **Equip** re-wears it (refused in combat, reports skipped);
      **To bar** drops a `preset:` token; a preset survives a relog (`EquipPresetsJson`).

**Dungeon (0.28.51):**
- [ ] A gatekeeper lists **Hollow Crypt**; teleporting there lands at the entrance safe zone; the elite
      rooms + boss are present at level 44–48.

**Regions (0.28.52, 0.28.54):**
- [ ] Crossing into a field/town shows the **"You entered X"** banner (with the level band for fields).
- [ ] Region **outlines** on the ground follow the zone-colours toggle; towns are muted steel-blue,
      fields amber, and **no field overlaps a town**.

**Leaderboards + break reminder (0.28.54):**
- [ ] **Menu → Rank** opens five boards (Level / Wealth / PvP / PK / Time played); each lists the top
      players; the #1 shows an honorary title; empty boards say so.
- [ ] After ~3h online a **"take a break"** banner appears (hard to test quickly — trust the wiring).

**All commands as buttons (0.28.55):**
- [ ] Target a mob → **Attack** / **Info** buttons. Target a player → **Attack / Follow / Assist /
      Party / Trade**; never on yourself. Each does what it says.
- [ ] The same actions placed on the **bar** work too (Trade / Party / Follow / Assist / Target-closest
      no longer say "not available on the phone").
- [ ] **Target-closest** selects the nearest enemy; pressing again steps to the next.
- [ ] Party window (as leader): **Kick** and **Lead** (change leader) buttons act on a member.

## 14. World pass — fields, walls, negative quadrant (0.28.56 → 0.28.61) — NOT YET TESTED (2026-07-24)

**Fields (Setup → zone colours toggle ON):**
- [ ] The map shows **filled colour FIELDS** (green→red by level), NOT the old spawn circles, wrapping
      each town; the **town sits inside as a neutral "island"**. Field OUTLINES + the muted-blue town
      outlines draw on top.
- [ ] The **Training Grounds** is one field over the four dummies; **Sunken Vale** (boss field, south) and
      **Hollow Crypt** (dungeon) read as fields too. Only the far lone spawns still show as circles.
- [ ] Crossing a boundary shows the **"You entered X"** banner (level band for fields).

**Walls + negative quadrant (dungeons/jail live at minus coords, reached by teleport):**
- [ ] You **can't walk off the overworld** into negative space — walking toward x<0 or y<0 stops at the
      edge; you can't reach the jail on foot.
- [ ] A **gatekeeper teleports you to Hollow Crypt** (the dungeon) — you arrive at the entrance safe zone
      and can walk into the crypt.
- [ ] Inside the dungeon you **can't walk back out** — movement is confined to the dungeon.
- [ ] (Admin) `/jail` a test char → they're pinned to the jail cell (negative coords) and can't wander out.

## 15. Rune shots (0.28.62 → 0.28.64) — NOT YET TESTED (2026-07-24)

The old always-on training passive is GONE; shots are held RUNE items now. **Admin (`admin/admin`)** starts
with both **30-day** shot boxes for testing.

- [ ] Open a **Soulshot Box** (admin has the 30d; or buy a **1h/2h** at the Apothecary) → you get a
      **Soulshot Rune** in the bag and a **Soulshot buff appears on the bar** with a countdown.
- [ ] With the rune, a fighter's **physical damage roughly DOUBLES**; a **Spiritshot Rune** boosts a
      mage's spell damage + cast speed. Wrong-type does nothing (soulshot on a mage's spells = no effect).
- [ ] The rune **can't be deleted** (bin refuses — "move it to the warehouse"); it **can't be traded**.
      The 1h/2h *boxes* CAN be traded; the 24h/30d boxes cannot.
- [ ] **Relog** → the rune is still there with the buff, its remaining time reduced by the time away
      (wall-clock — it counts down offline). Let a short one lapse → the rune vanishes + buff drops.
- [ ] A brand-new character's starter kit is **class-agnostic**: an **Armor choice box** (pick Fighter or
      Mage set), a **Weapons box** (pick ONE of 5, incl. staff), jewels, and a **1-day shot-rune choice
      box** (pick Soulshot or Spiritshot). All bound.

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
