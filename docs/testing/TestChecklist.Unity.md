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
required. Two APKs currently sit in `builds/`: **`L2Clone.apk` is the current one**;
`L2CloneMmorpg.apk` is an older product name and may install as a SECOND icon — decide which to keep.

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
- [ ] **Logout** (next to "Create character") returns to the **Sign in** panel, the phase goes
      `Offline`, and the password field is empty.
- [ ] After a Logout it does **not** silently sign you back in — the reconnect handler's cached
      credentials must have been cleared. Wait ~30s on the login screen to be sure.
- [ ] Logging in as a **different account** after a Logout shows THAT account's characters, not the
      previous one's (the connection is dropped on logout, so no server session can leak across).

## 4. The world renders — this is what was broken

The client used to subscribe to `"Snapshot"` while the server only sends `"SnapshotDelta"`, so the
world was **permanently empty**. These items exist to prove that path end to end.

- [ ] 🔴 **Your own entity is there the moment you enter** — the self panel shows name/Lv/HP/MP, NOT
      `waiting for your entity …`. **This regressed on 2026-07-21** (mobs rendered, you didn't).
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
- [ ] As an **admin/moderator** character, `/`-commands reach the server and take effect. The login
      line in the log ends with `[Admin]` / `[Moderator]`.
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

## Known gaps (NOT bugs — not built yet in the Unity client)

Don't file these; they're scope, not defects. The Unity client is a viewport, not the WPF harness.

- No inventory, skills, skill bar, buffs, quests, party, trade, shops, or NPC dialog UI.
- No skill casting at all — basic attack only.
- No cast bars, damage numbers, or death overlay.
- Entities are coloured billboards, not models; no animation.
- The camera is fixed (`CameraRig.Pitch` = 78°, near top-down like the WPF view); no rotate/zoom/pinch.
  It was briefly 55° for a 2.5D look — **that is a taste call, not a bug**; say which you want.
- UI is IMGUI (`OnGUI`), chosen so it needs no Canvas/prefabs/fonts — **it is meant to look plain**.
  A uGUI + TextMeshPro pass belongs with the real art pass.
