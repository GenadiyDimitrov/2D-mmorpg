# Unity client — build & run guide

Everything needed to get the mobile client onto a phone and talking to the server. For what the
client *is*, see [`Game.Client.Unity/README.md`](../../Game.Client.Unity/README.md); for what to
verify once it's running, see [the Unity test checklist](../testing/TestChecklist.Unity.md).

The whole Unity project is committed (Assets, scene, ProjectSettings, Packages/manifest). Only
generated things are ignored — `Library/`, `.utmp/`, `Assets/Packages/` (NuGet restores it) and
`Assets/Plugins/Game.Shared.dll` (the server build copies it in).

Unity is **6000.3.19f1 (Unity 6)**. §1–§4 at the bottom are ONE-TIME setup, already done on the
owner's machine — **for day-to-day work you only need §0.**

---

# §0. Daily run — step by step

Follow this top to bottom to get the phone playing. Steps 1–2 are once per PC reboot; 3–5 every run.

### 1. Plug the phone in and open the tunnel  *(once per reboot / re-plug)*

`adb reverse` tunnels the **phone's** `localhost:5238` to **your PC's** over the cable — no Wi-Fi, no
LAN IP, no firewall rule. adb ships with Unity's Android module:

```bash
export ADB="/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"

"$ADB" devices -l                    # must list the phone as "device"
"$ADB" reverse tcp:5238 tcp:5238
```

- `unauthorized` → the "Allow USB debugging?" prompt is still waiting **on the phone's screen**.
- **The tunnel dies on unplug, reboot, or an adb restart, and it fails silently** — the client just
  can't connect. Re-run the `reverse` line after any of those.

### 2. Restart Unity if you haven't since 2026-07-20

`ProjectSettings` changed `activeInputHandler` to `2` (Both). **Unity only reads it at startup** —
until you restart the Editor, legacy `Input.*` throws and **tapping does nothing**.

### 3. Start the server

```bash
dotnet run --project Game.Server
```

Leave it running in its own terminal. It binds `0.0.0.0:5238`, so the WPF client and the phone can
both reach it at once.

### 4. Put the app on the phone

**File → Build Settings → Android** (Switch Platform if it isn't already) → **Build And Run**.
That compiles the APK, installs it over USB and launches it.

- First build is slow (IL2CPP). Later builds are much faster.
- **Skip Editor Play mode** if it's still unstable on this PC — Build And Run doesn't use it at all.
- If the build fails, the APK simply never appears; read `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
  A bare "Build And Run does nothing" has meant a *failed build* here before, not a launch problem.

Already installed and unchanged? Just relaunch it instead of rebuilding:

```bash
"$ADB" shell monkey -p com.UnityTechnologies.com.unity.template.urpblank -c android.intent.category.LAUNCHER 1
```

> That package name is still the **URP template's default** (Player Settings → Other Settings →
> Identification). Fine for dev, but change it before any release — and note that changing it makes
> Android treat the app as a *different* app, so uninstall the old one or you'll have two icons.

### 5. In the app

1. The **Sign in** panel appears. Server should read `http://127.0.0.1:5238/game` (with the §1 tunnel).
2. **Register** a fresh account the first time, **Login** after that. URL + username are remembered.
3. **Create character** (name, tap to cycle Race/Class) → **Enter**.
4. Check the **top strip**: green dot and `frames N @ ~10.0/s` means the server feed is live.
   Red dot or `no frames yet` = not connected; tap **Log** to see why.

### Watching it from the PC while you play

```bash
"$ADB" logcat -c                     # clear, so you see only this run
"$ADB" logcat -s Unity               # only Unity's lines
```

Everything the on-screen console shows also lands here, plus native crashes.

---

# Running a SECOND client (party, trade, PvP, whisper…)

Any test with two actors needs two clients. **Two different accounts** — the server rejects the same
character logging in twice ("character is already online").

**Best pairing: phone (Unity) + PC (WPF).** Both connect to the same server at once; the phone uses
the `adb reverse` tunnel, the WPF client uses plain `localhost`. Nothing extra to set up.

It is also the pairing that *earns* the most: WPF is the mature client and the known-good reference,
so when the phone shows something strange you can immediately tell **"the game is wrong"** from
**"the Unity client is wrong"** by looking at the same moment in WPF. A second phone couldn't do that.
It's also the only way to confirm the server truly believes what the phone shows — walk on the phone,
watch the character move in the WPF window.

Other options, in order of usefulness:

- **Two WPF windows** — for pure game/mechanics tests where the phone adds nothing. Cheapest.
- **Unity Editor Play mode + the phone** — two Unity clients, for Unity-vs-Unity behaviour.
  ⚠ Play mode has hard-crashed this PC before (the no-page-file issue); use it only once that's confirmed fixed.
- **A second Android device / emulator** — most faithful, most setup. An emulator uses
  `http://10.0.2.2:5238/game` instead of the tunnel.

---

# One-time setup (already done on this machine)

## 1. The Unity project
Unity **6000.3.19f1** via Unity Hub. The project already exists and is committed — just open this
folder from the Hub. (Historical note: it began as the **URP Blank** template, which is why
`Assets/Settings`, `TutorialInfo` and the URP asset files are here.)

## 2. Add the networking dependencies (the one fiddly part)
Unity can't reference `net8.0`, so it uses the **netstandard2.1** build of our shared code + the
SignalR client:

1. **Install NuGetForUnity** (package manager for NuGet DLLs inside Unity):
   Window → Package Manager → **+** → *Add package from git URL* →
   `https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity`
2. **NuGet → Manage NuGet Packages** → search & install **`Microsoft.AspNetCore.SignalR.Client`**
   (pulls its dependencies into `Assets/Packages`).
3. **Build our shared DLL for Unity — the copy is AUTOMATIC:**
   ```bash
   dotnet build Game.Shared/Game.Shared.csproj -c Release -f netstandard2.1
   ```
   That's it. A post-build target in `Game.Shared.csproj` (`CopyToUnityPlugins`) drops
   `Game.Shared.dll` + `.pdb` into `Game.Client.Unity/Assets/Plugins/`, creating the folder if needed.
   It runs on **every** netstandard2.1 build — including the plain `dotnet build Game.sln` you already
   run — so the Unity project simply never goes stale.

   There is nothing to re-copy when DTOs or formulas change; that manual step was the whole problem.
   Missing one copy doesn't look like a missed copy, it looks like a protocol mismatch, and you'd hunt
   it in the networking code.

   - Skip the copy: `dotnet build -p:UnityPluginCopy=false`
   - The target no-ops if `Game.Client.Unity/` isn't there, so a server-only checkout still builds.
   - Unity's `.meta` files next to the DLL are left alone — Unity owns them, and overwriting them
     loses the importer settings.

> IL2CPP/Android note: if a SignalR method gets stripped, add a `link.xml` preserving
> `Microsoft.AspNetCore.SignalR.Client` and `System.Text.Json`. Start with the default Mono/IL2CPP
> settings; only touch this if you hit a runtime "method not found".

## 3. The scene
`Assets/Scenes/SampleScene.unity` is committed and already wired: a **Ground** plane, a **Main
Camera** with `CameraRig`, and a **GameController** holding `EntityManager` + `GameBoot` +
`TouchInput`.

**`GameBoot` is the only component a scene actually needs.** If the `EntityManager`, `CameraRig`,
`TouchInput`, `GameHud` or `GroundGrid` is missing it creates one at runtime, so a scene can't be
half-wired into a silent black screen. (`UnityMainThreadDispatcher` self-creates too.)

## 4. Point it at your server
- **Emulator (Android Studio AVD):** `http://10.0.2.2:5238/game`
- **Real phone over USB (easiest — see 4a):** `http://localhost:5238/game`
- **Real phone on the same Wi-Fi:** `http://<your-PC-LAN-IP>:5238/game` (e.g. `http://192.168.1.20:5238/game`)

> **You can now just type the URL into the login screen** — it's saved to `PlayerPrefs` and reused on
> the next launch, so this is no longer an Editor-only setting.
>
> ⚠ Precedence, if you do set it in the Editor: `PlayerPrefs` (whatever you last typed on that device)
> beats the **scene's** serialized `GameBoot.ServerUrl`, which in turn beats the C# default in
> `GameBoot.cs`. So editing the C# default changes nothing for an existing scene — and once you've
> logged in on the phone, the scene value stops mattering there too.

### 4a. Cabled phone: `adb reverse` (skips Wi-Fi, LAN IP *and* the firewall)
If the phone is on a USB cable, don't bother with the LAN at all. `adb reverse` tunnels the
**phone's** `localhost:5238` over the cable to **your PC's** `localhost:5238`:

```bash
adb reverse tcp:5238 tcp:5238
```

Then set the client's Server URL to `http://localhost:5238/game` and run the server normally.
This removes all three of the Wi-Fi route's failure points at once:

- no LAN IP to look up (and no picking the right subnet),
- **no Windows firewall rule** for port 5238 — the one manual step the Wi-Fi route can't automate,
- no requirement that the phone be on the same Wi-Fi, or on Wi-Fi at all.

`adb` ships with Unity's Android module, so it's already on your disk even if it isn't on `PATH`:

```
C:\Program Files\Unity\Hub\Editor\<version>\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
```

Check the phone is actually visible first — `adb devices -l` should list it as `device`
(`unauthorized` means the "Allow USB debugging?" prompt is still waiting on the phone's screen).

> **The tunnel does NOT survive** unplugging the phone, rebooting it, or an adb server restart, and
> it fails silently — the client just can't connect. Re-run the `adb reverse` line after any of those.

**Two server-side gotchas for a real phone — both are now DONE for you:**

1. **LAN binding: already handled.** The server binds `http://0.0.0.0:5238` (all interfaces), so
   `localhost` keeps working for the WPF client *and* the phone can reach your PC. Just
   `dotnet run --project Game.Server` — no environment variable needed.

   On startup it prints the exact URL to paste into the Unity client:
   ```
   Unity/phone clients on this LAN: http://192.168.1.20:5238/game
   ```
   (If several are listed, pick the one on the same subnet as your phone.) You still need to **allow
   port 5238 through the Windows firewall** — that part is not automatable from here, and a blocked
   port looks exactly like a wrong IP: a connection that just times out.

   > This README used to say `ASPNETCORE_URLS=http://0.0.0.0:5238 dotnet run`. That never worked:
   > `Program.cs` hardcoded `UseUrls("http://localhost:5238")`, which silently beat the variable, so the
   > server stayed on loopback and the phone failed to connect with nothing in the log to explain it.
   > An explicit `ASPNETCORE_URLS` is honoured now, and `launchSettings.json` no longer sets
   > `applicationUrl` (that key is *also* delivered as `ASPNETCORE_URLS` and quietly won).

2. **Cleartext HTTP: already handled** in `Assets/Plugins/Android/AndroidManifest.xml`. Since Android 9
   plain HTTP is blocked, and our dev server is `http://…`. The fix is an **attribute on the
   `<application>` element**, not an element of its own — that's the bit that isn't obvious:

   ```xml
   <uses-permission android:name="android.permission.INTERNET" />

   <application android:usesCleartextTraffic="true">
       ...activities...
   </application>
   ```

   Without it the phone fails with a bare "cleartext not permitted" and no other clue.
   *(Dev only — put the server behind HTTPS for a real release and delete the attribute.)*

## 5. Run
See **§0** at the top — that's the every-run guide. Colours in world: **green = you, cyan = other
players, red = mobs, yellow = NPCs**.

> `GameBoot.AutoLogin` (Inspector) skips the login screen using the credentials on the component —
> convenient in the Editor, off by default so the phone always shows the real login flow.

**Diagnosing a phone build.** Three failures that look identical on screen — an app that starts and
never gets in — and how to tell them apart:

- **Wrong URL / no tunnel** → the on-screen log and the login panel both show a connection refusal
  (the client spells out the `adb reverse` / LAN-IP hint). Remember the tunnel dies on unplug.
- **Version mismatch** → login is *refused politely*: "Client out of date (vX). Please update to vY."
  That means the plugin DLL is stale — rebuild `Game.Shared` (§2.3) and rebuild the APK.
- **IL2CPP stripping** → a "method not found" for a SignalR or `System.Text.Json` member in
  `adb logcat`, NOT a network error. Fix with the `link.xml` noted at the end of §2.

If the screen is black with **no UI at all**, it's none of the above — the scripts failed to compile
or threw in `Awake`. Go straight to `adb logcat -s Unity`.

---

## Going 2.5D later (the promised one-liner)
Select the Main Camera and lower **`CameraRig.Pitch`** from ~78 toward **~50**, and bump *Distance*.
The upright billboards tilt into an angled view with **zero** other changes. When you swap the quad
billboards for animated 3D models, it's a pure visual upgrade in `EntityManager.Create` /
`EntityView` — the networking, coordinates, camera and input all stay exactly as they are.

## What's in / what's next
- **In:** connect + auth + character select/create + enter world; the **delta** feed → billboards
  with interpolation; follow camera; tap move/attack; login + HUD UI (HP/MP/XP, target panel,
  nameplates with HP bars, on-screen log console, chat). `NetworkChannel` also exposes
  UseSkill/SetMoveState/etc. for later.
- **Next:** skill bar + casting, cast bars (incl. the mob cast bar), inventory/quest/shop screens,
  party window, then the 3D-model swap.
- **UI tech:** IMGUI (`OnGUI`) on purpose — it needs no Canvas, prefabs or font assets, so it can be
  authored outside the Editor and works on device with zero scene wiring. **It is meant to look
  plain.** A uGUI + TextMeshPro pass belongs with the real art pass, not before.
