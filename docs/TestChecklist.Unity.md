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

- [ ] **Unity was RESTARTED after this batch.** `ProjectSettings/ProjectSettings.asset` changed
      `activeInputHandler` from `1` (New Input System only) to `2` (Both). Unity only reads that at
      startup. Until you restart, legacy `Input.*` still throws and **tapping does nothing**.
- [ ] Server is running (`dotnet run --project Game.Server`), listening on `http://localhost:5238`.
- [ ] **Cabled phone:** `adb reverse tcp:5238 tcp:5238` has been re-run **since the last reboot/unplug**
      (the tunnel does not survive either), and the URL is `http://127.0.0.1:5238/game`.
      **Same Wi-Fi instead:** use the PC's LAN IP and make sure the firewall allows 5238.
- [ ] adb: `C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`

---

## 1. It starts and tells you something

The whole point of this batch: **before it, a failure was indistinguishable from a black screen.**

- [ ] The app launches and the **top status strip** is visible immediately: a coloured dot, the phase
      (`Offline`), `no frames yet`, `entities 0`, and the version `v0.27.0` on the right.
- [ ] The **Sign in** panel is on screen with Server / Username / Password fields.
- [ ] Text is **finger-sized**, not hairline — the UI scales to the screen's short side.
- [ ] Tapping a field opens the Android **soft keyboard** and typing works.
- [ ] The **Log** button (top right) opens a console panel; it already contains
      `Client v0.27.0 ready. Server: …`.

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

## 4. The world renders — this is what was broken

The client used to subscribe to `"Snapshot"` while the server only sends `"SnapshotDelta"`, so the
world was **permanently empty**. These items exist to prove that path end to end.

- [ ] The status strip shows **`frames N @ ~10.0/s`** and the dot is **green**. 10/s is the server
      tick reaching the phone.
- [ ] **`entities` is > 0** and roughly matches what the WPF client sees standing in the same spot.
- [ ] **You** are a green billboard, with the camera on you.
- [ ] Mobs are **red**, NPCs **yellow**, other players **cyan**.
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
- [ ] Chat: type in the console's field, tap **Say**, and the line reaches the **WPF client**.

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

## Known gaps (NOT bugs — not built yet in the Unity client)

Don't file these; they're scope, not defects. The Unity client is a viewport, not the WPF harness.

- No inventory, skills, skill bar, buffs, quests, party, trade, shops, or NPC dialog UI.
- No skill casting at all — basic attack only.
- No cast bars, damage numbers, or death overlay.
- Entities are coloured billboards, not models; no animation.
- The camera is fixed overhead (`CameraRig.Pitch` ≈ 78°); no rotate/zoom/pinch.
- UI is IMGUI (`OnGUI`), chosen so it needs no Canvas/prefabs/fonts — **it is meant to look plain**.
  A uGUI + TextMeshPro pass belongs with the real art pass.
